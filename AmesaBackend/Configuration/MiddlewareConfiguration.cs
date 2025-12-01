using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Middleware;
using Serilog;

namespace AmesaBackend.Configuration;

public static class MiddlewareConfiguration
{
    /// <summary>
    /// Configures Kestrel to use HTTPS in development (required for OAuth).
    /// </summary>
    public static IWebHostBuilder UseMainKestrel(this IWebHostBuilder webHostBuilder, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            webHostBuilder.UseKestrel(options =>
            {
                // Listen on HTTPS port 5001
                options.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.UseHttps();
                });
                // Also listen on HTTP port 5000 for backwards compatibility
                options.ListenLocalhost(5000);
            });
        }

        return webHostBuilder;
    }

    /// <summary>
    /// Configures the HTTP request pipeline including middleware, routing, authentication, Blazor Server, and endpoints.
    /// </summary>
    public static WebApplication UseMainMiddleware(this WebApplication app, IConfiguration configuration, IHostEnvironment environment)
    {
        // Configure the HTTP request pipeline
        // Enable Swagger in all environments for testing
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Amesa Lottery API V1");
            c.RoutePrefix = "swagger";
        });

        // Add CORS early in pipeline (before other middleware)
        // Must be before UseRouting() for preflight requests to work
        // CORS must be before error handling to ensure headers are sent on errors
        app.UseCors("AllowFrontend");

        // Add custom middleware
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Add response compression
        app.UseResponseCompression();

        // Add routing first (required for Blazor Server)
        app.UseRouting();

        // Add authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Add Session (after routing for Blazor Server compatibility)
        app.UseSession();

        // Serve static files for Blazor
        app.UseStaticFiles();

        // Add health checks endpoint
        app.MapHealthChecks("/health");

        // Map controllers
        app.MapControllers();

        // SignalR hubs moved to microservices:
        // - /ws/lottery -> AmesaBackend.Lottery (amesa-lottery-service)
        // - /ws/notifications -> AmesaBackend.Notification (amesa-notification-service)

        // Map Blazor Admin Panel
        app.MapBlazorHub();
        app.MapRazorPages(); // This is needed for Razor Pages
        app.MapFallbackToPage("/admin", "/Admin/App");
        app.MapFallbackToPage("/admin/{*path:nonfile}", "/Admin/App");

        return app;
    }
}


