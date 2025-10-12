using AmesaBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Services
{
    /// <summary>
    /// Service for managing database connections in admin panel
    /// Displays current environment information (no switching needed)
    /// </summary>
    public class AdminDatabaseService : IAdminDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public AdminDatabaseService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public string GetCurrentEnvironment()
        {
            // Determine environment based on deployment configuration
            // Dev and Staging share the same infrastructure, so we treat them as one
            
            // Check for explicit environment variable
            var envFromVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrEmpty(envFromVar))
            {
                return envFromVar switch
                {
                    "Production" => "Production",
                    _ => "Development" // Development and Staging both map to "Development"
                };
            }
            
            // Check database connection string to infer environment
            var connectionString = GetConnectionString();
            if (connectionString.Contains("amesadbmain-stage", StringComparison.OrdinalIgnoreCase))
            {
                return "Development"; // Dev and Stage share the same database
            }
            else if (connectionString.Contains("amesadbmain.cluster", StringComparison.OrdinalIgnoreCase) && 
                     !connectionString.Contains("amesadbmain-stage", StringComparison.OrdinalIgnoreCase))
            {
                return "Production";
            }
            
            // Default fallback
            return "Development";
        }

        private string GetConnectionString()
        {
            // Get connection string from environment variables first
            var envConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(envConnectionString))
            {
                return envConnectionString;
            }

            // Fallback to configuration
            return _configuration.GetConnectionString("DefaultConnection") ?? "Data Source=amesa.db";
        }

        public async Task<AmesaDbContext> GetDbContextAsync()
        {
            try
            {
                var connectionString = GetConnectionString();
                
                var optionsBuilder = new DbContextOptionsBuilder<AmesaDbContext>();
                
                // Determine if we're using PostgreSQL or SQLite based on connection string
                if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
                {
                    // PostgreSQL connection
                    optionsBuilder.UseNpgsql(connectionString);
                }
                else
                {
                    // SQLite connection
                    optionsBuilder.UseSqlite(connectionString);
                }
                
                // Enable sensitive data logging in development
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
                
                var context = new AmesaDbContext(optionsBuilder.Options);
                
                // Test the connection first
                await context.Database.OpenConnectionAsync();
                
                // Ensure the context is properly initialized
                await context.Database.EnsureCreatedAsync();
                
                return context;
            }
            catch (Exception ex)
            {
                // Throw a more descriptive error for the admin panel
                throw new InvalidOperationException(
                    $"Failed to connect to {GetCurrentEnvironment()} database. " +
                    $"Error: {ex.Message}. " +
                    $"Please check your network connection and ensure the PostgreSQL server is accessible.", ex);
            }
        }
    }
}