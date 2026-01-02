using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using AmesaBackend.Payment.Data;
using Serilog;

namespace AmesaBackend.Payment.Configuration;

public static class DatabaseConfiguration
{
    /// <summary>
    /// Configures Entity Framework with PostgreSQL for the Payment service.
    /// </summary>
    public static IServiceCollection AddPaymentDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<PaymentDbContext>(options =>
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

            // Never enable sensitive data logging in production
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
            var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            try
            {
                if (environment.IsDevelopment())
                {
                    Log.Information("Development mode: Ensuring Payment database tables are created...");
                    await context.Database.EnsureCreatedAsync();
                    Log.Information("Payment database setup completed successfully");
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
