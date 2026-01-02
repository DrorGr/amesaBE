using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Shared.Middleware.Extensions;

namespace AmesaBackend.Content.Configuration;

public static class MiddlewareConfiguration
{
    /// <summary>
    /// Configures the HTTP request pipeline including middleware, routing, authentication, and endpoints.
    /// </summary>
    public static WebApplication UseContentMiddleware(this WebApplication app, IConfiguration configuration, IHostEnvironment environment)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        // Enable X-Ray tracing if configured
        if (configuration.GetValue<bool>("XRay:Enabled", false))
        {
            // X-Ray tracing removed for microservices
        }

        app.UseAmesaSecurityHeaders(); // Security headers (before other middleware)
        app.UseAmesaMiddleware();
        app.UseAmesaLogging();

        app.UseRouting();

        // Add CORS early in pipeline (before UseAuthentication)
        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapControllers();

        return app;
    }
}
