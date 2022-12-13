using Microsoft.AspNetCore.Builder;

namespace Salix.AspNetCore.JsonExceptionHandler
{
    /// <summary>
    /// Extensions to register error handling middleware.
    /// </summary>
    public static class ServicesExtension
    {
        /// <summary>
        /// Registers default provided JSON Exception handler.<br/>
        /// All exceptions are treated as 500.<br/>
        /// Normally should use own class based on <see cref="ApiJsonExceptionMiddleware"/> with implemented <see cref="ApiJsonExceptionMiddleware.HandleSpecialException"/> method.
        /// </summary>
        public static IApplicationBuilder AddJsonExceptionHandler(this IApplicationBuilder app)
        {
            app.UseMiddleware<DefaultJsonExceptionHandler>(new ApiJsonExceptionOptions { OmitSources = new HashSet<string> { "DefaultJsonExceptionHandler" }, ShowStackTrace = true });
            return app;
        }

        /// <summary>
        /// Registers given implementation of JSON Exception handler with default options.<br/>
        /// Normally should use own class based on <see cref="ApiJsonExceptionMiddleware"/> with implemented <see cref="ApiJsonExceptionMiddleware.HandleSpecialException"/> method.
        /// </summary>
        public static IApplicationBuilder AddJsonExceptionHandler<THandler>(this IApplicationBuilder app) where THandler : ApiJsonExceptionMiddleware
        {
            app.UseMiddleware<THandler>(new ApiJsonExceptionOptions { OmitSources = new HashSet<string> { "JsonExceptionHandler" }, ShowStackTrace = true });
            return app;
        }

        /// <summary>
        /// Registers given implementation of JSON Exception handler.
        /// </summary>
        public static IApplicationBuilder AddJsonExceptionHandler<THandler>(this IApplicationBuilder app, ApiJsonExceptionOptions options) where THandler : ApiJsonExceptionMiddleware
        {
            app.UseMiddleware<THandler>(options);
            return app;
        }

        /// <summary>
        /// Registers given implementation of JSON Exception handler.
        /// </summary>
        public static IApplicationBuilder AddJsonExceptionHandler<THandler>(this IApplicationBuilder app, bool showStackTrace) where THandler : ApiJsonExceptionMiddleware
        {
            app.UseMiddleware<THandler>(showStackTrace);
            return app;
        }
    }
}
