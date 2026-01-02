using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Shared.Middleware.Extensions;
using AmesaBackend.Admin.Middleware;
using AmesaBackend.Admin.Hubs;

namespace AmesaBackend.Admin.Configuration;

public static class MiddlewareConfiguration
{
    /// <summary>
    /// Configures the HTTP request pipeline including middleware, routing, authentication, and endpoints.
    /// </summary>
    public static WebApplication UseAdminMiddleware(this WebApplication app, IConfiguration configuration, IHostEnvironment environment)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        // Configure path base for /admin routing from ALB
        // When ALB routes /admin/* to this service, we need to handle the path base
        app.UsePathBase("/admin");

        app.UseStaticFiles();

        // Enable X-Ray tracing if configured
        if (configuration.GetValue<bool>("XRay:Enabled", false))
        {
            // X-Ray tracing removed for microservices
        }

        app.UseAmesaSecurityHeaders(); // Security headers (before other middleware)
        app.UseGlobalExceptionHandler(); // Global error handling
        app.UseAmesaMiddleware();
        app.UseAmesaLogging();

        app.UseRouting();

        // Add session middleware (before authentication)
        app.UseSession();

        // Add authentication and authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Health check endpoint (before other routes)
        app.MapHealthChecks("/health");

        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapHub<AdminHub>("/hub"); // Changed from /admin/hub since UsePathBase handles /admin prefix

        // Map API controllers (for diagnostics endpoints)
        app.MapControllers();

        app.MapFallbackToPage("/_Host");

        return app;
    }
}
