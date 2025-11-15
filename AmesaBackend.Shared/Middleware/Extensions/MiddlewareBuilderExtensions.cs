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
    }
}

