using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Salix.AspNetCore.JsonExceptionHandler;

/// <summary>
/// Handles API errors (HTTP code > 399) as special Error object returned from API.
/// </summary>
[DebuggerDisplay("Global Error Handler")]
public abstract class ApiJsonExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiJsonExceptionOptions _options;
    private readonly ApiErrorFactory _apiErrorFactory = new();

    private static readonly JsonSerializerOptions ErrorSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    /// <summary>
    /// Logger instance.
    /// </summary>
    protected ILogger<ApiJsonExceptionMiddleware> Logger { get; }

    /// <summary>
    /// Middleware for intercepting unhandled exceptions and returning error object with appropriate status code.
    /// </summary>
    /// <param name="next">The next configured middleware in chain (setup in Startup.cs).</param>
    /// <param name="logger">The logger.</param>
    /// <param name="showStackTrace">
    /// Shows stack trace records.
    /// Usually this should be taken from IsDevelopment from Api.Hosting environment.
    /// Pass constant "true" to show stack trace always.
    /// </param>
    /// <exception cref="ArgumentNullException">Next step is not defined (should not ever happen).</exception>
    protected ApiJsonExceptionMiddleware(RequestDelegate next, ILogger<ApiJsonExceptionMiddleware> logger, bool showStackTrace = false)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        this.Logger = logger;
        _options = new ApiJsonExceptionOptions
        {
            ShowStackTrace = showStackTrace
        };
        _options.OmitSources.Add("ApiJsonExceptionMiddleware.cs");
    }

    /// <summary>
    /// Middleware for intercepting unhandled exceptions and returning error object with appropriate status code.
    /// </summary>
    /// <param name="next">The next configured middleware in chain (setup in Startup.cs).</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">Set options for Error Handling.</param>
    /// <exception cref="ArgumentNullException">Next step is not defined (should not ever happen).</exception>
    protected ApiJsonExceptionMiddleware(RequestDelegate next, ILogger<ApiJsonExceptionMiddleware> logger, ApiJsonExceptionOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        this.Logger = logger;
        _options = options;
        _options.OmitSources.Add("ApiJsonExceptionMiddleware.cs");
    }

    /// <summary>
    /// Overridden method which gets invoked by HTTP middleware stack.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <exception cref="ArgumentNullException">HTTP Context does not exist (never happens).</exception>
#pragma warning disable RCS1046 // Suffix Async is not expected by ASP.NET Core implementation
    public async Task Invoke(HttpContext httpContext)
#pragma warning restore RCS1046
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception exc)
        {
            // We can't do anything if the response has already started, just abort.
            if (httpContext.Response.HasStarted)
            {
                this.Logger.LogError(exc, "Unhandled exception occurred of type {ExceptionType} with message: \"{ExceptionMessage}\". Response started - no JSON handler is launched!", exc.GetType().Name, exc.Message);
                throw;
            }

            //var errorObject = this.CreateErrorObject(exc, httpContext.Response?.StatusCode);
            var errorObject = _apiErrorFactory.CreateErrorObject(exc, httpContext.Response?.StatusCode);
            errorObject.RequestedUrl = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget ?? httpContext.Request?.Path.ToString();
            errorObject = HandleSpecialException(errorObject, exc);
            errorObject = _apiErrorFactory.AddInnerExceptions(errorObject, exc);
            errorObject = _apiErrorFactory.AddStackTrace(errorObject, exc, _options);

            if (errorObject.ErrorBehavior.HasFlag(ApiErrorBehavior.LogError))
            {
                if (errorObject.ErrorType == ApiErrorType.DataValidationError)
                {
                    this.Logger.LogError(exc, "Data validation exception occurred with message: \"{ExceptionMessage}\".", exc.Message);
                }
                else
                {
                    this.Logger.LogError(
                        exc,
                        "Unhandled exception occurred of type {ExceptionType} with message: \"{ExceptionMessage}\".",
                        exc.GetType().Name,
                        exc.Message);
                }
            }

            if (errorObject.ErrorBehavior.HasFlag(ApiErrorBehavior.RespondWithError))
            {
                await WriteExceptionAsync(
                    httpContext,
                    errorObject,
                    errorObject.Status > 399 ? errorObject.Status : (errorObject.ErrorType == ApiErrorType.DataValidationError ? 400 : 500))
                    .ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Handler for Data Validation exception in API.
    /// Should be overridden in implementing class to fill with necessary data into ApiError object.
    /// </summary>
    /// <param name="apiError">Prepared API Error (as reference) to append any handled error information.</param>
    /// <param name="exception">Exception thrown in application.</param>
    /// <returns>Updated <paramref name="apiError"/>.</returns>
#pragma warning disable IDE0060 // Remove unused parameter - will be overridden.
    protected virtual ApiError HandleSpecialException(ApiError apiError, Exception exception) => apiError;

    /// <summary>
    /// Writes the exception as JSON object to requester back asynchronously.
    /// </summary>
    /// <param name="context">The HTTP context (where Request and Response resides).</param>
    /// <param name="errorData">The prepared error data object.</param>
    /// <param name="responseCode">HTTP Response code to use for error.</param>
    private static async Task WriteExceptionAsync(HttpContext context, ApiError errorData, int responseCode = 500)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        response.StatusCode = responseCode;
        var reason = response.HttpContext.Features.Get<IHttpResponseFeature>();
        if (reason != null)
        {
            reason.ReasonPhrase = null;
        }

        response.Headers.Clear();
        response.Headers[HeaderNames.CacheControl] = "no-cache";
        response.Headers[HeaderNames.Pragma] = "no-cache";
        response.Headers[HeaderNames.Expires] = "-1";
        response.Headers.Remove(HeaderNames.ETag);

        if (response.Body.CanSeek)
        {
            response.Body.SetLength(0L);
        }

        await response.WriteAsync(JsonSerializer.Serialize(errorData, ErrorSerializationOptions));
    }
}
