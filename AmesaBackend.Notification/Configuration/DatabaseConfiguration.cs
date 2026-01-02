using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using AmesaBackend.Notification.Data;
using Serilog;

namespace AmesaBackend.Notification.Configuration;

public static class DatabaseConfiguration
{
    /// <summary>
    /// Configures Entity Framework with PostgreSQL for the Notification service.
    /// Includes connection pooling and timeout settings.
    /// </summary>
    public static IServiceCollection AddNotificationDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<NotificationDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? configuration.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                // Add connection pool and timeout parameters
                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    MaxPoolSize = 100,
                    MinPoolSize = 10,
                    ConnectionLifetime = 300, // 5 minutes
                    CommandTimeout = 30, // 30 seconds
                    Timeout = 15 // Connection timeout in seconds
                };
                
                options.UseNpgsql(connectionStringBuilder.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    
                    // Add explicit command timeout
                    npgsqlOptions.CommandTimeout(30);
                });
            }

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Ensures the database is created (development only).
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this WebApplication app, IHostEnvironment environment)
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            try
            {
                if (environment.IsDevelopment())
                {
                    Log.Information("Development mode: Ensuring Notification database tables are created...");
                    await context.Database.EnsureCreatedAsync();
                    Log.Information("Notification database setup completed successfully");
                }
                else
                {
                    Log.Information("Production mode: Skipping EnsureCreated (use migrations)");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while setting up the database");
            }
        }
    }
}
