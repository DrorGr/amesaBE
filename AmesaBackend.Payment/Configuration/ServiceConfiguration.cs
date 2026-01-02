using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.Services.Interfaces;
using AmesaBackend.Payment.Services.ProductHandlers;
using AmesaBackend.Auth.Services;
using AmesaBackend.Shared.Extensions;

namespace AmesaBackend.Payment.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Controllers and JSON options (camelCase serialization).
    /// </summary>
    public static IServiceCollection AddPaymentControllers(this IServiceCollection services)
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
    /// Registers all application services including payment services and infrastructure services.
    /// </summary>
    public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Add shared services with Redis required for rate limiting
        services.AddAmesaBackendShared(configuration, environment, requireRedis: true);

        // Add Circuit Breaker Service (required by RateLimitService)
        services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();

        // Add Rate Limit Service (required by PaymentRateLimitService)
            services.AddScoped<IPaymentRateLimitService, PaymentRateLimitService>();

        // Payment Services
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentRateLimitService, PaymentRateLimitService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<ICoinbaseCommerceService, CoinbaseCommerceService>();
        services.AddScoped<IPaymentAuditService, PaymentAuditService>();

        // Product Handlers
        services.AddScoped<IProductHandler, LotteryTicketProductHandler>();
        services.AddSingleton<IProductHandlerRegistry>(serviceProvider =>
        {
            var registry = new ProductHandlerRegistry();
            var lotteryHandler = serviceProvider.GetRequiredService<IProductHandler>();
            registry.RegisterHandler(lotteryHandler);
            return registry;
        });

        // Add CORS policy for frontend access
        services.AddAmesaCors(configuration);

        // Add Health Checks
        services.AddHealthChecks();

        return services;
    }
}
