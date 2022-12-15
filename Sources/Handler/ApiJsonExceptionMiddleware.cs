using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Salix.StackTracer;

namespace Salix.AspNetCore.JsonExceptionHandler;

/// <summary>
/// Handles API errors (HTTP code > 399) as special Error object returned from API.
/// </summary>
[DebuggerDisplay("Global Error Handler")]
public abstract class ApiJsonExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiJsonExceptionOptions _options;

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
    public ApiJsonExceptionMiddleware(RequestDelegate next, ILogger<ApiJsonExceptionMiddleware> logger, bool showStackTrace = false)
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
    public ApiJsonExceptionMiddleware(RequestDelegate next, ILogger<ApiJsonExceptionMiddleware> logger, ApiJsonExceptionOptions options)
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

            var errorObject = this.CreateErrorObject(exc, httpContext.Response?.StatusCode);
            errorObject.RequestedUrl = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget ?? httpContext.Request?.Path.ToString();

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
                    errorObject.Status > 399 ? errorObject.Status : errorObject.ErrorType == ApiErrorType.DataValidationError ? 400 : 500)
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
    /// Creates the error data object from exception.
    /// </summary>
    /// <param name="exception">The exception which caused problems.</param>
    /// <param name="statusCode">The initial response status code.</param>
    private ApiError CreateErrorObject(Exception exception, int? statusCode)
    {
        var errorData = new ApiError
        {
            Title = exception.Message,
            ExceptionType = exception.GetType().Name,
            ErrorType = ApiErrorType.ServerError,
            Status = statusCode.HasValue && statusCode.Value > 399 ? statusCode.Value : 500
        };

        // All special exceptions are to be handled by overriding class.
        errorData = this.HandleSpecialException(errorData, exception);

        errorData.InnerException = this.GetInnerExceptionData(exception.InnerException);
        if (_options.ShowStackTrace)
        {
            // As there can be any kind of errors retrieving stack trace
            try
            {
                errorData.StackTrace = GetStackTrace(exception, _options.OmitSources);
            }
            catch (Exception ex)
            {
                errorData.StackTrace = new List<string> { "Error getting original stack trace: " + ex.Message };
            }
        }

        return errorData;
    }

    private ApiErrorInner? GetInnerExceptionData(Exception? innerException) =>
        innerException == null
            ? null
            : new ApiErrorInner
            {
                Title = innerException.Message,
                ExceptionType = innerException.GetType().Name,
                InnerException = this.GetInnerExceptionData(innerException.InnerException)
            };

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
        response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = null;

        response.Headers.Clear();
        response.Headers[HeaderNames.CacheControl] = "no-cache";
        response.Headers[HeaderNames.Pragma] = "no-cache";
        response.Headers[HeaderNames.Expires] = "-1";
        response.Headers.Remove(HeaderNames.ETag);

        if (response.Body.CanSeek)
        {
            response.Body.SetLength(0L);
        }

        await response.WriteAsync(JsonSerializer.Serialize(errorData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
    }

    /// <summary>
    /// Gets the stack trace of exception in suitable format.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="omitContaining">List of partial strings, which will act as filter to skip frames containing these strings.</param>
    private static List<string> GetStackTrace(Exception exception, HashSet<string> omitContaining)
    {
        var frames = new List<string>();
        if (exception == null)
        {
            return frames;
        }

        var stackTrace = exception.ParseStackTrace(new StackTracerOptions { SkipFramesWithoutLineNumber = true, SkipFramesContaining = omitContaining });
        foreach (var frame in stackTrace)
        {
            frames.Add(frame.ToString());
        }

        return frames;
    }
}
