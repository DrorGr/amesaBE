using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.BackgroundServices;
using AmesaBackend.Shared.Extensions;
using Amazon.Rekognition;
using Serilog;

namespace AmesaBackend.Auth.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Controllers and JSON options (camelCase serialization).
    /// </summary>
    public static IServiceCollection AddAuthControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Configure JSON serialization to use camelCase for property names
                // This ensures frontend receives properties in camelCase (e.g., "success" instead of "Success")
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
            });
        services.AddEndpointsApiExplorer();

        return services;
    }

    /// <summary>
    /// Configures CORS policy for the frontend.
    /// </summary>
    public static IServiceCollection AddAuthCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
                Log.Information("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromHours(24));
            });
        });

        return services;
    }

    /// <summary>
    /// Registers all application services including security services, auth services, AWS services, and infrastructure services.
    /// </summary>
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Add Shared Library Services
        // Auth service requires Redis for AccountLockoutService, RateLimitService, and EmailVerificationMiddleware
        services.AddAmesaBackendShared(configuration, environment, requireRedis: true);

        // Add AWS Services
        var awsRegion = configuration["Aws:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-north-1";
        services.AddSingleton<IAmazonRekognition>(sp =>
        {
            var region = Amazon.RegionEndpoint.GetBySystemName(awsRegion);
            return new AmazonRekognitionClient(region);
        });

        // Add Application Services
        // Security services
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<IAccountLockoutService, AccountLockoutService>();
        services.AddScoped<IPasswordValidatorService, PasswordValidatorService>();
        services.AddScoped<ICaptchaService, CaptchaService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Auth services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        // TODO: UserPreferencesService implementation missing
        // services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IAwsRekognitionService, AwsRekognitionService>();
        services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
        services.AddHttpContextAccessor();

        // Note: reCAPTCHA Enterprise uses Google Cloud API client, not HttpClient
        // Google Cloud credentials should be configured via GOOGLE_APPLICATION_CREDENTIALS environment variable
        // or Application Default Credentials (ADC) in AWS

        // Add Memory Cache
        services.AddMemoryCache();

        // Add HttpClient for Notification service sync
        services.AddHttpClient<INotificationPreferencesSyncService, NotificationPreferencesSyncService>()
            .ConfigureHttpClient((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var baseUrl = config["NotificationService:BaseUrl"] 
                    ?? Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL")
                    ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });

        // Register Notification Preferences Sync Service
        services.AddScoped<INotificationPreferencesSyncService, NotificationPreferencesSyncService>();

        // Add Health Checks
        services.AddHealthChecks();

        // Add Background Services
        services.AddHostedService<SessionCleanupService>();

        // Add Response Compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        return services;
    }
}

