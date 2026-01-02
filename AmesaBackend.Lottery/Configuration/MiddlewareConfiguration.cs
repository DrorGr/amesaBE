using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Shared.Middleware.Extensions;
using Serilog;

namespace AmesaBackend.Lottery.Configuration;

public static class MiddlewareConfiguration
{
    /// <summary>
    /// Configures the HTTP request pipeline including middleware, routing, authentication, and endpoints.
    /// </summary>
    public static WebApplication UseLotteryMiddleware(this WebApplication app, IConfiguration configuration, IHostEnvironment environment)
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

        // Add CORS early in pipeline (before routing)
        app.UseCors("AllowFrontend");

        app.UseResponseCaching(); // Must be before UseRouting for VaryByQueryKeys to work
        app.UseRouting();

        // Debug routing middleware (development only)
        if (app.Environment.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                var method = context.Request.Method;
                var path = context.Request.Path;
                
                Log.Debug("Request: {Method} {Path}", method, path);
                
                await next();
                
                if (context.Response.StatusCode == 405)
                {
                    Log.Warning("405 Method Not Allowed: {Method} {Path}", method, path);
                }
            });
        }

        app.UseAuthentication();

        // Service-to-service authentication middleware
        app.UseMiddleware<AmesaBackend.Shared.Middleware.ServiceToServiceAuthMiddleware>();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapControllers();

        // Map SignalR hubs
        app.MapHub<AmesaBackend.Lottery.Hubs.LotteryHub>("/ws/lottery");

        return app;
    }
}
