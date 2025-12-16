using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AmesaBackend.Shared.Extensions
{
    /// <summary>
    /// Extension methods for configuring CORS policies across all microservices.
    /// </summary>
    public static class CorsExtensions
    {
        /// <summary>
        /// Adds CORS policy for frontend access (CloudFront and localhost).
        /// This should be used by all microservices that need to be accessed from the frontend.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAmesaCors(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    // Get allowed origins from configuration
                    // Default: CloudFront production URL and localhost for development
                    var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() 
                        ?? new[] 
                        { 
                            "https://dpqbvdgnenckf.cloudfront.net",  // Production CloudFront
                            "http://localhost:4200"                   // Development Angular
                        };

                    Log.Information("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()  // GET, POST, PUT, DELETE, OPTIONS, PATCH
                          .AllowAnyHeader()  // Authorization, Content-Type, etc.
                          .AllowCredentials()  // Required for cookies and JWT tokens
                          .SetPreflightMaxAge(TimeSpan.FromHours(24));  // Cache preflight requests
                });
            });

            return services;
        }
    }
}

