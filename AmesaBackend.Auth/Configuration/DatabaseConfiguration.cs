using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using AmesaBackend.Auth.Data;
using Serilog;

namespace AmesaBackend.Auth.Configuration;

public static class DatabaseConfiguration
{
    /// <summary>
    /// Configures Entity Framework with PostgreSQL for the Auth service.
    /// Includes retry on failure, development SQL logging, and sensitive data logging.
    /// </summary>
    public static IServiceCollection AddAuthDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<AuthDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                if (environment.IsDevelopment())
                {
                    var devConfig = new ConfigurationBuilder()
                        .SetBasePath(environment.ContentRootPath)
                        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                        .Build();
                    
                    connectionString = devConfig.GetConnectionString("DefaultConnection");
                }
            }

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
            
            // Log all EF Core SQL queries to diagnose the deleted_at column error
            options.LogTo((message) =>
            {
                // Log all SQL queries and commands
                if (message.Contains("Executing") || message.Contains("Executed") || message.Contains("Failed"))
                {
                    Log.Information("[SQL DEBUG] {Message}", message);
                }
            }, new[] { 
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting,
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted,
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError
            });
        });

        return services;
    }

    /// <summary>
    /// Ensures the database is created (development only).
    /// In production, use migrations instead.
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this WebApplication app, IHostEnvironment environment)
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            try
            {
                if (environment.IsDevelopment())
                {
                    Log.Information("Development mode: Ensuring Auth database tables are created...");
                    await context.Database.EnsureCreatedAsync();
                    Log.Information("Auth database setup completed successfully");
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






