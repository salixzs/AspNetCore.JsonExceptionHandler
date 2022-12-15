using Salix.AspNetCore.JsonExceptionHandler;

namespace JsonExceptionHandler.Sample.Api
{
    public class MyCustomJsonExceptionMiddleware : ApiJsonExceptionMiddleware
    {
        public MyCustomJsonExceptionMiddleware(RequestDelegate next, ILogger<ApiJsonExceptionMiddleware> logger, ApiJsonExceptionOptions options) : base(next, logger, options)
        {
        }

        protected override ApiError HandleSpecialException(ApiError apiError, Exception exception)
        {
            if (exception is ApplicationException)
            {
                apiError.ErrorType = ApiErrorType.NotImplemented;
                apiError.ErrorBehavior = ApiErrorBehavior.RespondWithError;
                apiError.Status = 422;
            }

            return apiError;
        }
    }
}
