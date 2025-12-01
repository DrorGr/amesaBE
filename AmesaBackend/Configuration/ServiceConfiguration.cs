using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Services;
using Serilog;

namespace AmesaBackend.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures CORS policy for the frontend.
    /// </summary>
    public static IServiceCollection AddMainCors(this IServiceCollection services, IConfiguration configuration)
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
    /// Registers all application services including AutoMapper, Blazor Server, Session, and background services.
    /// </summary>
    public static IServiceCollection AddMainServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Add AutoMapper
        services.AddAutoMapper(typeof(Program));

        // Add Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILotteryService, LotteryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IQRCodeService, QRCodeService>();

        // Add Admin Panel Services
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminDatabaseService, AdminDatabaseService>();
        services.AddHttpContextAccessor();

        // Add Session for Admin Panel
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(2);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Name = "AmesaAdmin.Session";
        });

        // Add Blazor Server for Admin Panel
        services.AddRazorPages();
        services.AddServerSideBlazor();

        // Add Background Services
        services.AddHostedService<LotteryDrawService>();
        services.AddHostedService<NotificationBackgroundService>();

        // SignalR hubs moved to microservices:
        // - LotteryHub moved to AmesaBackend.Lottery
        // - NotificationHub moved to AmesaBackend.Notification

        // Add Memory Cache
        services.AddMemoryCache();

        // Add Distributed Cache (Redis in production) - Disabled for local development
        // if (environment.IsProduction())
        // {
        //     services.AddStackExchangeRedisCache(options =>
        //     {
        //         options.Configuration = configuration.GetConnectionString("Redis");
        //     });
        // }

        // Add Health Checks
        services.AddHealthChecks();

        // Add Response Compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        return services;
    }
}

