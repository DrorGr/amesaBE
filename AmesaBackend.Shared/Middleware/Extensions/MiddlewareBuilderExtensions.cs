using AmesaBackend.Shared.Middleware.ErrorHandling;
using AmesaBackend.Shared.Middleware.Logging;
using Microsoft.AspNetCore.Builder;

namespace AmesaBackend.Shared.Middleware.Extensions
{
    public static class MiddlewareBuilderExtensions
    {
        public static IApplicationBuilder UseAmesaMiddleware(
            this IApplicationBuilder builder)
        {
            builder.UseMiddleware<ErrorHandlerMiddleware>();
            return builder;
        }

        public static IApplicationBuilder UseAmesaLogging(
            this IApplicationBuilder builder)
        {
            builder.UseMiddleware<AMESASerilogScopedLoggingMiddleware>();
            builder.UseMiddleware<RequestResponseLoggingMiddleware>();
            return builder;
        }

        /// <summary>
        /// Adds security headers middleware to the pipeline.
        /// Should be called early in the middleware pipeline, before routing.
        /// </summary>
        public static IApplicationBuilder UseAmesaSecurityHeaders(
            this IApplicationBuilder builder)
        {
            builder.UseMiddleware<SecurityHeadersMiddleware>();
            return builder;
        }
    }
}

