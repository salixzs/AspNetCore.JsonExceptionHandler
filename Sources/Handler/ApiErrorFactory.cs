using Salix.StackTracer;

namespace Salix.AspNetCore.JsonExceptionHandler
{
    internal class ApiErrorFactory
    {
        /// <summary>
        /// Creates the error data object from exception.
        /// </summary>
        /// <param name="exception">The exception which caused problems.</param>
        /// <param name="statusCode">The initial response status code.</param>
#pragma warning disable CA1822 // Mark members as static
        internal ApiError CreateErrorObject(Exception exception, int? statusCode) => new()
        {
            Title = exception.Message,
            ExceptionType = exception.GetType().Name,
            ErrorType = ApiErrorType.ServerError,
            Status = statusCode > 399 ? statusCode.Value : 500
        };

        /// <summary>
        /// Appends inner exceptions to ApiError.
        /// </summary>
        /// <param name="initialError">Initially created and modified Api Error object.</param>
        /// <param name="exception">The exception which caused problems.</param>
        internal ApiError AddInnerExceptions(ApiError initialError, Exception exception)
        {
            initialError.InnerException = this.GetInnerExceptionData(exception.InnerException);
            return initialError;
        }

        /// <summary>
        /// Appends inner exceptions to ApiError.
        /// </summary>
        /// <param name="initialError">Initially created and modified Api Error object.</param>
        /// <param name="exception">The exception which caused problems.</param>
        /// <param name="options">Handler options</param>
        internal ApiError AddStackTrace(ApiError initialError, Exception exception, ApiJsonExceptionOptions options)
        {
            if (!options.ShowStackTrace)
            {
                return initialError;
            }

            try
            {
                initialError.StackTrace = GetStackTrace(exception, options.OmitSources);
            }
            catch (Exception ex)
            {
                initialError.StackTrace = ["Error getting original stack trace: " + ex.Message];
            }

            return initialError;
        }
#pragma warning restore CA1822 // Mark members as static

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

            foreach (var frame in exception.ParseStackTrace(new StackTracerOptions { SkipFramesWithoutLineNumber = true, SkipFramesContaining = omitContaining }))
            {
                frames.Add(frame.ToString());
            }

            return frames;
        }
    }
}
