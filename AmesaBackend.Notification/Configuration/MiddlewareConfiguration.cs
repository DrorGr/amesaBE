using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Shared.Middleware.Extensions;
using AmesaBackend.Notification.Hubs;
using Serilog;

namespace AmesaBackend.Notification.Configuration;

public static class MiddlewareConfiguration
{
    /// <summary>
    /// Configures the HTTP request pipeline including middleware, routing, authentication, and endpoints.
    /// </summary>
    public static WebApplication UseNotificationMiddleware(this WebApplication app, IConfiguration configuration, IHostEnvironment environment)
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

        app.UseRouting();

        // Extract JWT token from query string for SignalR HTTP requests (negotiate endpoint)
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var method = context.Request.Method;
            var isWsPath = path.StartsWithSegments("/ws");
            var accessTokenQuery = context.Request.Query["access_token"];
            var accessToken = accessTokenQuery.ToString();
            var hasToken = !string.IsNullOrWhiteSpace(accessToken);
            var existingAuthHeader = context.Request.Headers["Authorization"].ToString();
            var hasAuthHeader = !string.IsNullOrWhiteSpace(existingAuthHeader);

            // For SignalR negotiate requests, extract token from query string if not in header
            if (isWsPath && hasToken && !hasAuthHeader)
            {
                // Add token to Authorization header for JWT middleware
                context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
            }
            
            await next();
        });

        app.UseAuthentication();
        app.UseAuthorization();

        // Basic health check for ALB - only checks if service is running
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Name == "basic",
            ResultStatusCodes = {
                [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = Microsoft.AspNetCore.Http.StatusCodes.Status200OK,
                [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = Microsoft.AspNetCore.Http.StatusCodes.Status200OK,
                [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = Microsoft.AspNetCore.Http.StatusCodes.Status503ServiceUnavailable
            }
        });

        // Detailed health check with all channels - for monitoring/debugging
        app.MapHealthChecks("/health/notifications", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        exception = e.Value.Exception?.Message,
                        data = e.Value.Data
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        app.MapControllers();

        // Map SignalR hubs
        app.MapHub<NotificationHub>("/ws/notifications");

        return app;
    }
}
