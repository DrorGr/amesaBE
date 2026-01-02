using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Notification.Services;
using AmesaBackend.Notification.Services.Interfaces;
using AmesaBackend.Notification.Services.Background;
using AmesaBackend.Notification.Services.Channels;
using AmesaBackend.Notification.Handlers;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Shared.Extensions;
using Amazon.SimpleEmail;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.CloudWatch;
using Telegram.Bot;
using Serilog;

namespace AmesaBackend.Notification.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Controllers and JSON options (camelCase serialization).
    /// </summary>
    public static IServiceCollection AddNotificationControllers(this IServiceCollection services)
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
    /// Registers all application services including notification services, AWS services, and infrastructure services.
    /// </summary>
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Add shared services with Redis required for RateLimitService
        services.AddAmesaBackendShared(configuration, environment, requireRedis: true);

        // Add Circuit Breaker Service (required by RateLimitService)
        services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();

        // Rate limiting service (shared from Auth service)
        services.AddScoped<IRateLimitService, RateLimitService>();

        // Core notification services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailService, EmailService>();

        // AWS SDK clients
        var emailConfig = configuration.GetSection("NotificationChannels:Email");
        var smsConfig = configuration.GetSection("NotificationChannels:SMS");
        var emailRegion = Amazon.RegionEndpoint.GetBySystemName(emailConfig["Region"] ?? "eu-north-1");
        var smsRegion = Amazon.RegionEndpoint.GetBySystemName(smsConfig["Region"] ?? "eu-north-1");

        services.AddSingleton<IAmazonSimpleEmailService>(sp =>
            new AmazonSimpleEmailServiceClient(emailRegion));
        services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
            new AmazonSimpleNotificationServiceClient(smsRegion));

        // SQS client for notification queue
        var sqsRegion = Amazon.RegionEndpoint.GetBySystemName(
            configuration["NotificationQueue:Region"] ?? "eu-north-1");
        services.AddSingleton<IAmazonSQS>(sp =>
            new AmazonSQSClient(sqsRegion));

        // Telegram Bot Client
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var botToken = config["NotificationChannels:Telegram:BotToken"];
            
            if (string.IsNullOrEmpty(botToken) || botToken == "FROM_SECRETS")
            {
                Log.Warning("Telegram bot token not configured. Telegram features will be limited.");
                return null!; // Return null but register as singleton - will be checked in usage
            }
            
            return new TelegramBotClient(botToken);
        });

        // CloudWatch client for metrics
        services.AddSingleton<IAmazonCloudWatch>(sp =>
            new AmazonCloudWatchClient(Amazon.RegionEndpoint.GetBySystemName("eu-north-1")));

        // Channel providers
        services.AddScoped<IChannelProvider, EmailChannelProvider>();
        services.AddScoped<IChannelProvider, SMSChannelProvider>();
        services.AddScoped<IChannelProvider, PushChannelProvider>();
        services.AddScoped<IChannelProvider, WebPushChannelProvider>();
        services.AddScoped<IChannelProvider, TelegramChannelProvider>();
        services.AddScoped<IChannelProvider, SocialMediaChannelProvider>();

        // Core services
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
        services.AddScoped<IDeviceRegistrationService, DeviceRegistrationService>();
        services.AddScoped<ITemplateEngine, TemplateEngine>();
        services.AddScoped<INotificationTypeMappingService, NotificationTypeMappingService>();
        services.AddScoped<INotificationReadStateService, NotificationReadStateService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<INotificationArchiveService, NotificationArchiveService>();
        services.AddScoped<ICloudWatchMetricsService, CloudWatchMetricsService>();

        // Service clients for cross-service communication
        services.AddScoped<ILotteryServiceClient, LotteryServiceClient>();
        services.AddScoped<IAuthServiceClient, AuthServiceClient>();

        // HttpContextAccessor for read state service
        services.AddHttpContextAccessor();

        // Add Background Service for EventBridge events
        services.AddHostedService<EventBridgeEventHandler>();

        // Add Background Service for notification queue processing
        services.AddHostedService<NotificationQueueProcessor>(sp =>
        {
            var serviceProvider = sp;
            var logger = sp.GetRequiredService<ILogger<NotificationQueueProcessor>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var sqsClient = sp.GetRequiredService<IAmazonSQS>();
            return new NotificationQueueProcessor(serviceProvider, logger, config, sqsClient);
        });

        // Add Background Service for notification archiving
        services.AddHostedService<NotificationArchiveBackgroundService>();

        // Add SignalR for real-time updates
        services.AddSignalR();

        // Register all health checks
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddHealthChecks()
            .AddCheck<AmesaBackend.Notification.HealthChecks.BasicHealthCheck>("basic")
            .AddCheck<AmesaBackend.Notification.HealthChecks.DatabaseHealthCheck>("database")
            .AddCheck<AmesaBackend.Notification.HealthChecks.EmailChannelHealthCheck>("email_channel")
            .AddCheck<AmesaBackend.Notification.HealthChecks.SMSChannelHealthCheck>("sms_channel")
            .AddCheck<AmesaBackend.Notification.HealthChecks.WebPushChannelHealthCheck>("webpush_channel")
            .AddCheck<AmesaBackend.Notification.HealthChecks.TelegramChannelHealthCheck>("telegram_channel");

        // Add CORS policy for frontend access
        services.AddAmesaCors(configuration);

        return services;
    }
}
