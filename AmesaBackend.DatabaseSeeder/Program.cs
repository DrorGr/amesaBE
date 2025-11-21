using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using AmesaBackend.Data;
using AmesaBackend.DatabaseSeeder.Models;
using AmesaBackend.DatabaseSeeder.Services;
using Npgsql;

namespace AmesaBackend.DatabaseSeeder
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Configure Serilog early
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Starting AmesaBackend Database Seeder...");

                // Build configuration
                var configuration = BuildConfiguration(args);

                // Create host
                using var host = CreateHost(configuration);

                // Get services
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                var seederService = host.Services.GetRequiredService<DatabaseSeederService_Fixed>();

                // Display configuration info
                var environment = configuration["SeederSettings:Environment"] ?? "Development";
                var connectionString = GetConnectionString(configuration, environment);
                
                logger.LogInformation("=== AmesaBackend Database Seeder ===");
                logger.LogInformation($"Environment: {environment}");
                logger.LogInformation($"Connection: {MaskConnectionString(connectionString)}");
                logger.LogInformation("=====================================");

                // Prompt for confirmation in production
                if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine();
                    Console.WriteLine("⚠️  WARNING: You are about to seed the PRODUCTION database!");
                    Console.WriteLine("This will TRUNCATE existing data and replace it with demo data.");
                    Console.WriteLine();
                    Console.Write("Are you sure you want to continue? (type 'YES' to confirm): ");
                    
                    var confirmation = Console.ReadLine();
                    if (confirmation != "YES")
                    {
                        logger.LogInformation("Operation cancelled by user");
                        return 1;
                    }
                }

                // Run seeding
                await seederService.SeedDatabaseAsync();

                logger.LogInformation("Database seeding completed successfully!");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Database seeding failed with an unhandled exception");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IConfiguration BuildConfiguration(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
        }

        private static IHost CreateHost(IConfiguration configuration)
        {
            var environment = configuration["SeederSettings:Environment"] ?? "Development";

            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddSerilog(new LoggerConfiguration()
                            .ReadFrom.Configuration(configuration)
                            .WriteTo.Console()
                            .CreateLogger());
                    });

                    // Configure database
                    var connectionString = GetConnectionString(configuration, environment);
                    
                    // Configure Npgsql for dynamic JSON support
                    NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
                    
                    services.AddDbContext<AmesaDbContext>(options =>
                    {
                        options.UseNpgsql(connectionString);
                        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                        {
                            options.EnableSensitiveDataLogging();
                            options.EnableDetailedErrors();
                        }
                    });

                    // Configure seeder settings
                    services.Configure<SeederSettings>(configuration.GetSection("SeederSettings"));

                    // Register services
                    services.AddScoped<DatabaseSeederService_Fixed>();
                })
                .Build();
        }

        private static string GetConnectionString(IConfiguration configuration, string environment)
        {
            // Try environment-specific connection string first
            var connectionString = environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? configuration.GetConnectionString("ProductionConnection")
                : configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string not found for environment: {environment}");
            }

            return connectionString;
        }

        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "Not configured";

            // Mask password in connection string for logging
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = "***";
            }
            return builder.ToString();
        }
    }
}
