#if NET8_0_OR_GREATER
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Salix.AspNetCore.JsonExceptionHandler
{
    /// <summary>
    /// <see cref="IExceptionHandler"/> implementation.<br/>
    /// Since .Net 8 there is new way to handle exception globally.
    /// See <a href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-8.0#iexceptionhandler">MS Docs</a>.
    /// Use it to create your own exception handler by deriving from this class.<br/>
    /// <code>
    /// public class MyExceptionHandler : ApiJsonExceptionHandler
    /// {
    ///     public UnhandledExceptionHandler(ILogger&lt;ApiJsonExceptionHandler&gt; logger)
    ///         : base(logger, new ApiJsonExceptionOptions { ShowStackTrace = true })
    ///
    ///     // If needed implement this method:
    ///     protected override ApiError HandleSpecialException(ApiError apiError, Exception exception)
    ///     {
    ///         if (exception is MyDefinedException)
    ///         {
    ///             apiError.ErrorType = ApiErrorType.StorageError;
    ///             apiError.ErrorBehavior = ApiErrorBehavior.Ignore;
    ///         }
    ///     }
    /// }
    /// </code>
    /// Then register it with DI.
    /// <code>
    /// builder.Services.AddExceptionHandler&lt;MyExceptionHandler&gt;();
    /// // then
    /// app.UseExceptionHandler(_ => { });
    /// </code>
    /// </summary>
    [DebuggerDisplay("Global Error Handler")]
    public abstract class ApiJsonExceptionHandler : IExceptionHandler
    {
        private readonly ApiErrorFactory _errorFactory = new();

        private readonly ILogger<ApiJsonExceptionHandler> _logger;

        private readonly ApiJsonExceptionOptions _options = new();

        /// <inheritdoc cref="ApiJsonExceptionHandler"/>
        protected ApiJsonExceptionHandler(ILogger<ApiJsonExceptionHandler> logger) => _logger = logger;

        /// <inheritdoc cref="ApiJsonExceptionHandler"/>
        protected ApiJsonExceptionHandler(ILogger<ApiJsonExceptionHandler> logger, ApiJsonExceptionOptions options)
        {
            _logger = logger;
            _options = options;
        }

        /// <inheritdoc/>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var apiError = _errorFactory.CreateErrorObject(exception, httpContext.Response?.StatusCode);
            apiError.RequestedUrl = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget ?? httpContext.Request?.Path.ToString();
            HandleSpecialException(apiError, exception);
            apiError = _errorFactory.AddInnerExceptions(apiError, exception);
            apiError = _errorFactory.AddStackTrace(apiError, exception, _options);

            if (apiError.ErrorBehavior.HasFlag(ApiErrorBehavior.LogError))
            {
                if (apiError.ErrorType == ApiErrorType.DataValidationError)
                {
                    _logger.LogError(exception, "Data validation exception occurred with message: \"{ExceptionMessage}\".", exception.Message);
                }
                else
                {
                    _logger.LogError(
                        exception,
                        "Unhandled exception occurred of type {ExceptionType} with message: \"{ExceptionMessage}\".",
                        exception.GetType().Name,
                        exception.Message);
                }
            }

            if (apiError.ErrorBehavior.HasFlag(ApiErrorBehavior.RespondWithError) && httpContext.Response != null)
            {
                httpContext.Response.StatusCode = apiError.Status;
                await httpContext.Response
                    .WriteAsJsonAsync(apiError, cancellationToken);
                return true;
            }

            return false;
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
    }
}
#endif
