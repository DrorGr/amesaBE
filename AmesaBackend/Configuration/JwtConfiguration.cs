using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace AmesaBackend.Configuration;

public static class JwtConfiguration
{
    /// <summary>
    /// Configures JWT Bearer authentication with cookie authentication support.
    /// Includes WebSocket token support for /ws endpoints.
    /// </summary>
    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // In Production, JWT SecretKey is loaded from AWS SSM Parameter Store via ECS task definition secrets
        // (environment variable: JwtSettings__SecretKey -> config: JwtSettings:SecretKey)
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        // Validate JWT SecretKey exists and is not a placeholder
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured. Set JwtSettings__SecretKey environment variable or configure JwtSettings:SecretKey in appsettings.");
        }

        // In Production, ensure we're not using placeholder values
        if (environment.IsProduction())
        {
            var placeholderValues = new[] 
            { 
                "your-super-secret-key-for-jwt-tokens-min-32-chars",
                "your-super-secret-key-that-is-at-least-32-characters-long"
            };
            
            if (placeholderValues.Any(p => secretKey.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("JWT SecretKey appears to be a placeholder. Ensure JwtSettings__SecretKey environment variable is set from AWS SSM Parameter Store.");
            }
            
            Console.WriteLine("[JWT] Using SecretKey from environment variable (SSM Parameter Store)");
        }
        else
        {
            Console.WriteLine("[JWT] Development mode - using SecretKey from appsettings.Development.json");
        }

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            // Don't set DefaultChallengeScheme to JwtBearer - OAuth challenges need to use their specific scheme
            // When Challenge() is called with a specific scheme (like Google), it will use that scheme
            options.DefaultSignInScheme = "Cookies"; // Required for OAuth sign-in
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddCookie("Cookies", options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                // Use Lax for localhost to allow cookies on redirects
                // In production with HTTPS, this should be None with Secure
                if (environment.IsDevelopment())
                {
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                }
                else
                {
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                }
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                // Handle token from query string for WebSocket connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return authBuilder;
    }
}

