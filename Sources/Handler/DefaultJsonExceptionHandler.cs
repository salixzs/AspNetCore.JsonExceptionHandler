using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Salix.AspNetCore.JsonExceptionHandler;

/// <summary>
/// Default provided exception handler when own (based on <see cref="ApiJsonExceptionMiddleware"/>) is notimplemented.
/// </summary>
public sealed class DefaultJsonExceptionHandler : ApiJsonExceptionMiddleware
{
    /// <summary>
    /// Default provided exception handler when own (based on <see cref="ApiJsonExceptionMiddleware"/>) is notimplemented.
    /// </summary>
    public DefaultJsonExceptionHandler(RequestDelegate next, ILogger<ApiJsonExceptionMiddleware> logger, ApiJsonExceptionOptions options)
        : base(next, logger, options)
    {
    }
}
