using AmesaBackend.Shared.Authentication;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Shared.Events;
using Amazon.EventBridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AmesaBackend shared services to the service collection
        /// </summary>
        public static IServiceCollection AddAmesaBackendShared(this IServiceCollection services, IConfiguration configuration)
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

            // Add Redis Cache - Required for production
            var redisConnection = configuration.GetConnectionString("Redis") 
                ?? configuration["CacheConfig:RedisConnection"]
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
            
            var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production" ||
                              configuration["ASPNETCORE_ENVIRONMENT"] == "Production";
            
            if (!string.IsNullOrEmpty(redisConnection))
            {
                services.UseRedisCache(configuration);
            }
            else if (isProduction)
            {
                throw new InvalidOperationException(
                    "Redis connection string is required in production. " +
                    "Set ConnectionStrings__Redis environment variable or configure CacheConfig:RedisConnection in appsettings.");
            }

            return services;
        }
    }
}

