using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace AmesaBackend.Payment.Configuration;

public static class JwtConfiguration
{
    /// <summary>
    /// Configures JWT Bearer authentication.
    /// </summary>
    public static AuthenticationBuilder AddPaymentJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] 
            ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            Log.Warning("JWT SecretKey is not configured. Authentication will not work. Set JwtSettings__SecretKey environment variable or configure JwtSettings:SecretKey in appsettings.");
            return services.AddAuthentication(); // Return empty builder if no secret
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
        });

        Log.Information("JWT Authentication configured successfully");
        return authBuilder;
    }
}
