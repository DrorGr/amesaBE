using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using AmesaBackend.Data;
using Serilog;

namespace AmesaBackend.Configuration;

public static class DatabaseConfiguration
{
    /// <summary>
    /// Configures Entity Framework with dual database provider support (PostgreSQL or SQLite).
    /// PostgreSQL is preferred; SQLite is fallback for development only.
    /// </summary>
    public static IServiceCollection AddMainDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<AmesaDbContext>(options =>
        {
            // Get connection string - prioritize environment variable, then Development config, then default
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                // In Development, explicitly load from appsettings.Development.json
                if (environment.IsDevelopment())
                {
                    var devConfig = new ConfigurationBuilder()
                        .SetBasePath(environment.ContentRootPath)
                        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                        .Build();
                    
                    var devConnectionString = devConfig.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrEmpty(devConnectionString))
                    {
                        connectionString = devConnectionString;
                        Console.WriteLine("[DB Config] ✅ Loaded connection string from appsettings.Development.json");
                    }
                }
                
                // Fallback to base config if not found in Development config
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = configuration.GetConnectionString("DefaultConnection");
                }
            }
            
            // Debug output to console
            var connectionPreview = connectionString != null && connectionString.Length > 50 
                ? connectionString.Substring(0, 50) + "..." 
                : connectionString ?? "NULL";
            Console.WriteLine($"[DB Config] Connection string preview: {connectionPreview}");
            
            // Use PostgreSQL if connection string contains PostgreSQL format, otherwise SQLite
            if (connectionString != null && (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) || 
                                             connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)))
            {
                // PostgreSQL connection string detected
                Console.WriteLine("[DB Config] ✅ Using PostgreSQL database provider");
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });
            }
            else
            {
                // SQLite connection string (fallback only - not recommended for production)
                // Only use SQLite if explicitly requested via connection string format
                if (environment.IsDevelopment() && string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("[DB Config] ⚠️ WARNING: No connection string found. Using SQLite fallback.");
                    Console.WriteLine("[DB Config] ⚠️ For PostgreSQL, ensure appsettings.Development.json has correct connection string.");
                    connectionString = "Data Source=AmesaDB.db";
                }
                Console.WriteLine("[DB Config] ⚠️ Using SQLite database provider (PostgreSQL connection string not detected)");
                options.UseSqlite(connectionString ?? "Data Source=AmesaDB.db");
            }
            
            // Enable sensitive data logging in development
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
    /// In production, use migrations instead.
    /// NOTE: Database seeding is DISABLED - all seeding must be done manually.
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this WebApplication app, IHostEnvironment environment)
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AmesaDbContext>();
            try
            {
                if (environment.IsDevelopment())
                {
                    // Only ensure database schema exists in development - NO automatic seeding
                    // Database migrations should be run manually: dotnet ef database update
                    // Database seeding should be done manually using AmesaBackend.DatabaseSeeder project
                    Log.Information("Development mode: Ensuring database schema exists (no automatic seeding)...");
                    await context.Database.EnsureCreatedAsync();
                    Log.Information("Database schema check completed. No data was seeded automatically.");
                    Log.Information("⚠️  To seed data manually, use: dotnet run --project AmesaBackend.DatabaseSeeder");
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













