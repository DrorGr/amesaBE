using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Auth.Middleware;
using AmesaBackend.Auth.Services;
using AmesaBackend.Shared.Middleware.Extensions;
using Serilog;

namespace AmesaBackend.Auth.Configuration;

public static class MiddlewareConfiguration
{
    /// <summary>
    /// Configures forwarded headers for load balancer/proxy (required for HTTPS detection).
    /// </summary>
    public static IServiceCollection AddAuthForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            // Trust all proxies in production (ALB/CloudFront)
            // In a more secure setup, you'd whitelist specific proxy IPs
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    /// <summary>
    /// Configures Kestrel to use HTTPS in development (required for OAuth).
    /// </summary>
    public static IWebHostBuilder UseAuthKestrel(this IWebHostBuilder webHostBuilder, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            webHostBuilder.UseKestrel(options =>
            {
                options.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.UseHttps();
                });
                options.ListenLocalhost(5000);
            });
        }

        return webHostBuilder;
    }

    /// <summary>
    /// Configures the HTTP request pipeline including middleware, routing, authentication, and endpoints.
    /// Includes security middleware, CAPTCHA health endpoint, and all standard ASP.NET Core middleware.
    /// </summary>
    public static WebApplication UseAuthMiddleware(this WebApplication app, IConfiguration configuration, IHostEnvironment environment)
    {
        // Configure the HTTP request pipeline
        // Use forwarded headers BEFORE other middleware to ensure correct scheme detection
        app.UseForwardedHeaders();

        // Force HTTPS scheme in production for OAuth redirects
        // This ensures the OAuth redirect URI uses HTTPS even if the request comes as HTTP from the load balancer
        if (app.Environment.IsProduction())
        {
            app.Use(async (context, next) =>
            {
                // If request came through CloudFront/ALB, ensure scheme is HTTPS
                if (context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
                {
                    var proto = context.Request.Headers["X-Forwarded-Proto"].ToString();
                    if (proto.Equals("https", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Request.Scheme = "https";
                    }
                }
                // Also check CloudFront-specific headers
                else if (context.Request.Headers.ContainsKey("CloudFront-Forwarded-Proto"))
                {
                    var proto = context.Request.Headers["CloudFront-Forwarded-Proto"].ToString();
                    if (proto.Equals("https", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Request.Scheme = "https";
                    }
                }
                // For CloudFront domains, always use HTTPS
                else if (context.Request.Host.Host.Contains("cloudfront.net", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Scheme = "https";
                }
                
                await next();
            });
        }

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Amesa Auth API V1");
            c.RoutePrefix = "swagger";
        });

        // Add CORS early in pipeline
        app.UseCors("AllowFrontend");

        // Add CSRF protection via Origin header validation (after CORS, before authentication)
        app.UseOriginHeaderValidation();

        // Add shared middleware
        // Enable X-Ray tracing if configured
        if (configuration.GetValue<bool>("XRay:Enabled", false))
        {
            // X-Ray tracing removed for microservices
        }

        app.UseAmesaMiddleware();
        app.UseAmesaLogging();

        // Add custom security middleware (early in pipeline)
        app.UseSecurityHeaders(); // Add security headers to all responses
        app.UseMiddleware<IpTrackingMiddleware>();
        app.UseMiddleware<EmailVerificationMiddleware>();
        app.UseMiddleware<SecurityAuditMiddleware>();

        // Add response compression
        app.UseResponseCompression();

        // Add routing
        app.UseRouting();

        // Add authentication and authorization
        app.UseAuthentication();
        
        // Add session activity tracking (after authentication, before authorization)
        app.UseMiddleware<SessionActivityMiddleware>();
        
        app.UseAuthorization();

        // Add health checks endpoint
        app.MapHealthChecks("/health");

        // Add CAPTCHA metrics endpoint (for monitoring)
        app.MapGet("/health/captcha", () =>
        {
            var metrics = CaptchaService.GetMetrics();
            return Results.Json(new
            {
                totalAttempts = metrics.Total,
                successCount = metrics.Success,
                failureCount = metrics.Failures,
                successRate = metrics.SuccessRate,
                failureRate = metrics.Total > 0 ? 100 - metrics.SuccessRate : 0,
                status = metrics.SuccessRate >= 80 || metrics.Total < 10 ? "healthy" : "degraded"
            });
        });

        // Map controllers
        app.MapControllers();

        return app;
    }
}











