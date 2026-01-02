using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace AmesaBackend.Lottery.Configuration;

public static class JwtConfiguration
{
    /// <summary>
    /// Configures JWT Bearer authentication with SignalR WebSocket token support.
    /// </summary>
    public static AuthenticationBuilder AddLotteryJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] 
            ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured. Set JwtSettings__SecretKey environment variable or configure JwtSettings:SecretKey in appsettings.");
        }

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"] ?? "AmesaAuthService",
                ValidAudience = jwtSettings["Audience"] ?? "AmesaFrontend",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock difference for reliability
            };

            // Extract JWT token from query string for SignalR WebSocket connections
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                    {
                        context.Token = accessToken;
                        
                        // Only log in development for debugging
                        if (environment.IsDevelopment())
                        {
                            Log.Debug("SignalR token extracted from query string for path: {Path}", path);
                        }
                    }
                    else if (path.StartsWithSegments("/ws"))
                    {
                        // Log missing token only in development
                        if (environment.IsDevelopment())
                        {
                            Log.Warning("SignalR connection attempt without token on path: {Path}", path);
                        }
                    }
                    
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var path = context.HttpContext.Request.Path;
                    
                    // Log authentication failures with sanitized information
                    if (environment.IsDevelopment())
                    {
                        Log.Warning("SignalR authentication failed for path: {Path}, Error: {Error}",
                            path, context.Exception?.Message ?? "Unknown error");
                    }
                    else
                    {
                        // Production: Only log error type, not details
                        Log.Warning("SignalR authentication failed for WebSocket path");
                    }
                    
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Minimal logging in production
                    if (environment.IsDevelopment())
                    {
                        var path = context.HttpContext.Request.Path;
                        Log.Debug("SignalR authentication challenge for path: {Path}", path);
                    }
                    
                    return Task.CompletedTask;
                }
            };
        });

        return authBuilder;
    }
}
