using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using AmesaBackend.Analytics.Data;
using Serilog;

namespace AmesaBackend.Analytics.Configuration;

public static class DatabaseConfiguration
{
    /// <summary>
    /// Configures Entity Framework with PostgreSQL for the Analytics service.
    /// </summary>
    public static IServiceCollection AddAnalyticsDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<AnalyticsDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? configuration.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
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
            var context = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
            try
            {
                if (environment.IsDevelopment())
                {
                    Log.Information("Development mode: Ensuring Analytics database tables are created...");
                    await context.Database.EnsureCreatedAsync();
                    Log.Information("Analytics database setup completed successfully");
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
