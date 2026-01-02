using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Services.Interfaces;
using AmesaBackend.Lottery.Services.Background;
using AmesaBackend.Lottery.Services.Processors;
using LotteryDrawService = AmesaBackend.Lottery.Services.Background.LotteryDrawService;
using TicketQueueProcessorService = AmesaBackend.Lottery.Services.Background.TicketQueueProcessorService;
using ReservationCleanupService = AmesaBackend.Lottery.Services.Background.ReservationCleanupService;
using InventorySyncService = AmesaBackend.Lottery.Services.Background.InventorySyncService;
using LotteryCountdownService = AmesaBackend.Lottery.Services.Background.LotteryCountdownService;
using FavoritesCleanupService = AmesaBackend.Lottery.Services.Background.FavoritesCleanupService;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Shared.Extensions;
using Polly;
using Polly.Extensions.Http;
using Amazon.SQS;
using AmesaBackend.Lottery.Configuration;

namespace AmesaBackend.Lottery.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Controllers and JSON options (camelCase serialization).
    /// </summary>
    public static IServiceCollection AddLotteryControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
            });
        services.AddEndpointsApiExplorer();

        // Add Response Caching for [ResponseCache] attribute support
        services.AddResponseCaching();

        return services;
    }

    /// <summary>
    /// Registers all application services including lottery services, reservation services, and infrastructure services.
    /// </summary>
    public static IServiceCollection AddLotteryServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Lottery service requires Redis for house list caching and cache invalidation
        services.AddAmesaBackendShared(configuration, environment, requireRedis: true);

        // Configure Lottery settings
        services.Configure<LotterySettings>(configuration.GetSection("Lottery"));

        // Register CircuitBreakerService FIRST (required by RateLimitService)
        services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();

        // Register IRateLimitService from Auth service (requires ICircuitBreakerService)
        services.AddScoped(typeof(IRateLimitService), typeof(RateLimitService));

        // Application Services
        // Register UserPreferencesService for favorites functionality
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<ILotteryService, LotteryService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IPromotionAuditService, PromotionAuditService>();
        services.AddScoped<IErrorSanitizer, ErrorSanitizer>();
        services.AddScoped<IGamificationService, GamificationService>();
        services.AddScoped<AmesaBackend.Shared.Configuration.IConfigurationService, AmesaBackend.Lottery.Services.ConfigurationService>();

        // Reservation system services
        services.AddScoped<IRedisInventoryManager, RedisInventoryManager>();
        services.AddScoped<ITicketReservationService, TicketReservationService>();
        services.AddScoped<IReservationProcessor, ReservationProcessor>();
        services.AddScoped<IPaymentProcessor, PaymentProcessor>();
        services.AddScoped<ITicketCreatorProcessor, TicketCreatorProcessor>();

        // Register HttpClient for payment service with timeout, retry, and circuit breaker policies
        services.AddHttpClient<IPaymentProcessor, PaymentProcessor>(client =>
        {
            var paymentServiceUrl = configuration["PaymentService:BaseUrl"] 
                ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";
            client.BaseAddress = new Uri(paymentServiceUrl);
            var lotterySettings = configuration.GetSection("Lottery").Get<LotterySettings>() ?? new LotterySettings();
            client.Timeout = TimeSpan.FromSeconds(lotterySettings.Payment.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register SQS client
        services.AddSingleton<IAmazonSQS>(sp =>
        {
            var region = configuration["AWS:Region"] ?? "eu-north-1";
            return new AmazonSQSClient(Amazon.RegionEndpoint.GetBySystemName(region));
        });

        // Add Background Services
        services.AddHostedService<LotteryDrawService>();
        services.AddHostedService<TicketQueueProcessorService>();
        services.AddHostedService<ReservationCleanupService>();
        services.AddHostedService<InventorySyncService>();
        services.AddHostedService<LotteryCountdownService>();
        services.AddHostedService<FavoritesCleanupService>();

        // Add SignalR for real-time updates
        services.AddSignalR();

        // Add CORS policy for frontend access
        services.AddAmesaCors(configuration);

        // Add Health Checks
        services.AddHealthChecks();

        return services;
    }

    // Retry policy: Exponential backoff for transient HTTP errors
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempt (will be logged by PaymentProcessor)
                });
    }

    // Circuit breaker policy: Open circuit after 5 consecutive failures, break for 30 seconds
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
