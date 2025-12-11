using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.BackgroundServices;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Data;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Events;
using Amazon.Rekognition;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace AmesaBackend.Auth.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Controllers and JSON options (camelCase serialization).
    /// Also configures custom ModelState validation response to match ApiResponse format.
    /// </summary>
    public static IServiceCollection AddAuthControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // Custom response factory for ModelState validation errors
                // Returns ApiResponse format that frontend expects
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = new Dictionary<string, string[]>();
                    foreach (var keyValuePair in context.ModelState)
                    {
                        var key = keyValuePair.Key;
                        var errorMessages = keyValuePair.Value.Errors
                            .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value" : e.ErrorMessage)
                            .ToArray();
                        
                        if (errorMessages.Length > 0)
                        {
                            errors[key] = errorMessages;
                        }
                    }

                    // Format validation errors into a single message
                    var errorMessage = string.Join("; ", errors.SelectMany(e => e.Value));

                    var response = new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "VALIDATION_ERROR",
                            Message = errorMessage,
                            Details = errors // Include detailed field errors
                        },
                        Timestamp = DateTime.UtcNow
                    };

                    return new BadRequestObjectResult(response);
                };
            })
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
        // Infrastructure services
        services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
        
        // Security services
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<IAccountLockoutService, AccountLockoutService>();
        
        // Password breach checking service (with HttpClient)
        services.AddHttpClient<IPasswordBreachService, PasswordBreachService>();
        
        services.AddScoped<IPasswordValidatorService, PasswordValidatorService>();
        services.AddScoped<ICaptchaService, CaptchaService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Auth services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        
        // Register AccountRecoveryService with Lazy<IPasswordResetService> to break circular dependency
        services.AddScoped<IAccountRecoveryService>(sp =>
        {
            var context = sp.GetRequiredService<AuthDbContext>();
            var passwordResetService = new Lazy<IPasswordResetService>(() => sp.GetRequiredService<IPasswordResetService>());
            var emailVerificationService = sp.GetRequiredService<IEmailVerificationService>();
            var tokenService = sp.GetRequiredService<ITokenService>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<AccountRecoveryService>>();
            return new AccountRecoveryService(context, passwordResetService, emailVerificationService, tokenService, configuration, logger);
        });
        
        // Register PasswordResetService with Lazy<IAccountRecoveryService> to break circular dependency
        services.AddScoped<IPasswordResetService>(sp =>
        {
            var context = sp.GetRequiredService<AuthDbContext>();
            var eventPublisher = sp.GetRequiredService<IEventPublisher>();
            var passwordValidator = sp.GetRequiredService<IPasswordValidatorService>();
            var tokenService = sp.GetRequiredService<ITokenService>();
            var sessionService = sp.GetRequiredService<ISessionService>();
            var accountRecoveryService = new Lazy<IAccountRecoveryService>(() => sp.GetRequiredService<IAccountRecoveryService>());
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<PasswordResetService>>();
            return new PasswordResetService(context, eventPublisher, passwordValidator, tokenService, sessionService, accountRecoveryService, configuration, logger);
        });
        
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        // TODO: UserPreferencesService implementation missing
        // services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IAwsRekognitionService, AwsRekognitionService>();
        services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IAccountDeletionService, AccountDeletionService>();
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
        services.AddHostedService<PasswordHistoryCleanupService>();

        // Add Response Compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        return services;
    }
}

