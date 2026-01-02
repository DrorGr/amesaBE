using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using AmesaBackend.Admin.Data;
using AmesaBackend.Auth.Data;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Payment.Data;
using AmesaBackend.Content.Data;
using AmesaBackend.LotteryResults.Data;
using AmesaBackend.Notification.Data;
using Serilog;

namespace AmesaBackend.Admin.Configuration;

public static class DatabaseConfiguration
{
    /// <summary>
    /// Configures Entity Framework with PostgreSQL for all schemas in the Admin service.
    /// </summary>
    public static IServiceCollection AddAdminDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
            ?? configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Configure all DbContexts with schema-specific search paths
            services.AddDbContext<AuthDbContext>(options =>
                ConfigureDbContext(options, connectionString, "amesa_auth", environment));
            
            services.AddDbContext<LotteryDbContext>(options =>
                ConfigureDbContext(options, connectionString, "amesa_lottery", environment));
            
            services.AddDbContext<PaymentDbContext>(options =>
                ConfigureDbContext(options, connectionString, "amesa_payment", environment));
            
            services.AddDbContext<ContentDbContext>(options =>
                ConfigureDbContext(options, connectionString, "amesa_content", environment));
            
            services.AddDbContext<LotteryResultsDbContext>(options =>
                ConfigureDbContext(options, connectionString, "amesa_lottery_results", environment));
            
            services.AddDbContext<NotificationDbContext>(options =>
                ConfigureDbContext(options, connectionString, "amesa_notification", environment));
            
            services.AddDbContext<AdminDbContext>(options =>
                ConfigureDbContext(options, connectionString, "amesa_admin", environment), ServiceLifetime.Scoped);
        }
        else
        {
            // If no connection string, register AdminDbContext as null/optional
            // AdminAuthService will fallback to legacy config-based auth
            Log.Warning("Database connection string is not configured. Admin authentication will use legacy config-based auth only.");
        }

        return services;
    }

    // Helper method to configure DbContext with schema search path
    private static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString, string schema, IHostEnvironment environment)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = 100,
            MinPoolSize = 10,
            ConnectionLifetime = 300, // 5 minutes
            CommandTimeout = 30, // 30 seconds
            Timeout = 15, // Connection timeout in seconds
            SearchPath = schema // Set schema search path
        };
        
        options.UseNpgsql(connectionStringBuilder.ConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
            
            npgsqlOptions.CommandTimeout(30);
        });
        
        if (environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
        
        // Disable concurrency detection for read operations
        options.EnableThreadSafetyChecks(false);
    }
}
