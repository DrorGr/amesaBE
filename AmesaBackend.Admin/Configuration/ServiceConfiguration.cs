using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Admin.Services;
using AmesaBackend.Admin.Services.Interfaces;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Shared.Extensions;
using StackExchange.Redis;
using Amazon.S3;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatch;
using Serilog;
using Microsoft.AspNetCore.Http;

namespace AmesaBackend.Admin.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures Blazor Server services and controllers.
    /// </summary>
    public static IServiceCollection AddAdminControllers(this IServiceCollection services)
    {
        // Add services to the container.
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddControllers(); // For API endpoints (diagnostics)

        // Add HttpContextAccessor for session access in services
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Configures Redis for session storage with fallback to in-memory cache.
    /// </summary>
    public static IServiceCollection AddAdminSessionStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // CRITICAL: Distributed cache MUST be registered before AddSession()
        var redisConnection = configuration.GetConnectionString("Redis") 
            ?? configuration["CacheConfig:RedisConnection"]
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis");

        // ALWAYS register a distributed cache FIRST (required for session middleware)
        // This ensures session store is available even if Redis connection fails
        bool redisConfigured = false;
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            try
            {
                // Add Redis connection (lazy initialization to avoid blocking startup)
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    try
                    {
                        return ConnectionMultiplexer.Connect(redisConnection.Trim());
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to connect to Redis, service will use in-memory cache");
                        throw; // Re-throw to trigger fallback
                    }
                });
                
                // Configure distributed cache (used by session)
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection.Trim();
                    options.InstanceName = configuration["CacheConfig:InstanceName"] ?? "amesa-admin";
                });
                
                redisConfigured = true;
                Log.Information("Session storage configured to use Redis");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to configure Redis, falling back to in-memory cache");
                // Ensure we always have a distributed cache
                services.AddDistributedMemoryCache();
                redisConfigured = false;
            }
        }
        else
        {
            // Fallback to in-memory distributed cache if Redis is not configured
            services.AddDistributedMemoryCache();
            Log.Warning("Redis connection not configured. Using in-memory session storage (sessions will not persist across restarts)");
        }

        // Configure session (MUST be after distributed cache registration)
        // AddSession() requires IDistributedCache to be registered
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(120); // 2 hours
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax; // Better CSRF protection while allowing navigation
            // SECURITY: Always use HTTPS in production (behind ALB/CloudFront)
            options.Cookie.SecurePolicy = environment.IsDevelopment() 
                ? CookieSecurePolicy.SameAsRequest 
                : CookieSecurePolicy.Always;
        });

        return services;
    }

    /// <summary>
    /// Registers all application services including admin services, AWS services, and infrastructure services.
    /// </summary>
    public static IServiceCollection AddAdminServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Add shared services (may also configure Redis, but our distributed cache is already registered)
        // Pass requireRedis=false to prevent exceptions if Redis is not available
        services.AddAmesaBackendShared(configuration, environment, requireRedis: false);

        // Configure AWS Services
        var awsRegion = Amazon.RegionEndpoint.GetBySystemName(configuration["AWS:Region"] ?? "eu-north-1");
        services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(awsRegion));
        services.AddSingleton<IAmazonCloudWatchLogs>(sp => new AmazonCloudWatchLogsClient(awsRegion));
        services.AddSingleton<IAmazonCloudWatch>(sp => new AmazonCloudWatchClient(awsRegion));

        // Add Admin Services
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminDatabaseService, AdminDatabaseService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IHousesService, HousesService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<ITranslationsService, TranslationsService>();
        services.AddScoped<ITicketsService, TicketsService>();
        services.AddScoped<IDrawsService, DrawsService>();
        services.AddScoped<IPaymentsService, PaymentsService>();
        services.AddScoped<IS3ImageService, S3ImageService>();
        services.AddScoped<ICloudWatchLoggingService, CloudWatchLoggingService>();
        services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();

        // Configure health checks (simple endpoint that returns healthy if service is running)
        services.AddHealthChecks();

        return services;
    }
}
