using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Shared.Extensions;

namespace AmesaBackend.Content.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Controllers and JSON options (camelCase serialization).
    /// </summary>
    public static IServiceCollection AddContentControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
            });
        services.AddEndpointsApiExplorer();

        return services;
    }

    /// <summary>
    /// Registers all application services.
    /// </summary>
    public static IServiceCollection AddContentServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Content service requires Redis for translations and languages caching
        services.AddAmesaBackendShared(configuration, environment, requireRedis: true);

        // Add CORS policy for frontend access
        services.AddAmesaCors(configuration);

        // Add Health Checks
        services.AddHealthChecks();

        return services;
    }
}
