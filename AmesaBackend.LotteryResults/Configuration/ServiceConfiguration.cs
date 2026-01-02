using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.LotteryResults.Services;
using AmesaBackend.LotteryResults.Services.Interfaces;
using AmesaBackend.LotteryResults.Handlers;
using AmesaBackend.Shared.Extensions;

namespace AmesaBackend.LotteryResults.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Controllers and JSON options (camelCase serialization).
    /// </summary>
    public static IServiceCollection AddLotteryResultsControllers(this IServiceCollection services)
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
    public static IServiceCollection AddLotteryResultsServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddAmesaBackendShared(configuration, environment);

        // Add Services
        services.AddScoped<IQRCodeService, QRCodeService>();
        services.AddScoped<ILotteryResultsService, LotteryResultsService>();

        // Add Background Service for EventBridge events
        services.AddHostedService<LotteryDrawWinnerEventHandler>();

        // Add CORS policy for frontend access
        services.AddAmesaCors(configuration);

        // Add Health Checks
        services.AddHealthChecks();

        return services;
    }
}
