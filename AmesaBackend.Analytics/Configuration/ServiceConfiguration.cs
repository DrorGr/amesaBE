using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Analytics.Services;
using AmesaBackend.Analytics.Services.Interfaces;
using AmesaBackend.Shared.Extensions;

namespace AmesaBackend.Analytics.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Controllers and JSON options (camelCase serialization).
    /// </summary>
    public static IServiceCollection AddAnalyticsControllers(this IServiceCollection services)
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
    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddAmesaBackendShared(configuration, environment);

        // Add Services
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        // Add CORS policy for frontend access
        services.AddAmesaCors(configuration);

        // Add Health Checks
        services.AddHealthChecks();

        return services;
    }
}
