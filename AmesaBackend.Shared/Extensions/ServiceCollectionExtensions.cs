using AmesaBackend.Shared.Authentication;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Shared.Events;
using Amazon.EventBridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AmesaBackend.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AmesaBackend shared services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="environment">Optional host environment for production detection</param>
        /// <param name="requireRedis">Whether Redis is required for this service (default: false)</param>
        public static IServiceCollection AddAmesaBackendShared(
            this IServiceCollection services, 
            IConfiguration configuration,
            IHostEnvironment? environment = null,
            bool requireRedis = false)
        {
            // Add JWT Token Manager
            services.AddScoped<IJwtTokenManager, JwtTokenManager>();

            // Add HTTP Request Service
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddScoped<IHttpRequest, HttpRequestService>();

            // Add EventBridge
            services.AddAWSService<IAmazonEventBridge>();
            services.AddScoped<IEventPublisher, EventBridgePublisher>();

            // Add Redis Cache - Required for production if requireRedis is true
            var redisConnection = configuration.GetConnectionString("Redis") 
                ?? configuration["CacheConfig:RedisConnection"]
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
            
            // Trim whitespace if connection string is found
            redisConnection = redisConnection?.Trim();
            
            // Correct production detection: use IHostEnvironment if available, fallback to environment variable
            var isProduction = environment?.IsProduction() ?? 
                              Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
            
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                services.UseRedisCache(configuration, redisConnection);  // Pass the connection string we found
            }
            else if (isProduction && requireRedis)
            {
                throw new InvalidOperationException(
                    "Redis connection string is required in production for this service. " +
                    "Set ConnectionStrings__Redis environment variable or configure CacheConfig:RedisConnection in appsettings.");
            }

            return services;
        }
    }
}

