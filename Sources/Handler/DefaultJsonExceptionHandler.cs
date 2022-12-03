using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Salix.AspNetCore.JsonExceptionHandler;

public sealed class DefaultJsonExceptionHandler : ApiJsonExceptionMiddleware
{
    public DefaultJsonExceptionHandler(RequestDelegate next, ILogger<ApiJsonExceptionMiddleware> logger, ApiJsonExceptionOptions options)
        : base(next, logger, options)
    {
    }
}
