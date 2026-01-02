using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Shared.Middleware.Extensions;
using Serilog;

namespace AmesaBackend.Payment.Configuration;

public static class MiddlewareConfiguration
{
    /// <summary>
    /// Configures the HTTP request pipeline including middleware, routing, authentication, and endpoints.
    /// </summary>
    public static WebApplication UsePaymentMiddleware(this WebApplication app, IConfiguration configuration, IHostEnvironment environment)
    {
        // Security headers middleware (before other middleware)
        app.UseAmesaSecurityHeaders();

        // HTTPS redirection and HSTS (production only)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        // Enable X-Ray tracing if configured
        if (configuration.GetValue<bool>("XRay:Enabled", false))
        {
            // X-Ray tracing removed for microservices
        }

        app.UseAmesaMiddleware();
        app.UseAmesaLogging();

        // Add CORS early in pipeline (before routing)
        app.UseCors("AllowFrontend");

        app.UseRouting();

        // Only use authentication if JWT secret is available at runtime
        var jwtSecretAtRuntime = app.Configuration["JwtSettings:SecretKey"] 
            ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");
            
        if (!string.IsNullOrWhiteSpace(jwtSecretAtRuntime))
        {
            app.UseAuthentication();
            app.UseAuthorization();
            Log.Information("Authentication middleware enabled - JWT secret found");
        }
        else
        {
            Log.Warning("Skipping UseAuthentication() and UseAuthorization() - JWT secret not configured");
        }

        app.MapHealthChecks("/health");
        app.MapControllers();

        return app;
    }
}
