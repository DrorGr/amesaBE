using AmesaBackend.Notification.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Notification.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;
        
        public DatabaseHealthCheck(
            NotificationDbContext context,
            ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext healthCheckContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple query to verify database connectivity
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                
                if (!canConnect)
                {
                    return HealthCheckResult.Unhealthy("Database connection failed");
                }
                
                // Execute a simple query to verify database is operational
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                
                return HealthCheckResult.Healthy("Database is operational");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database health check failed", ex);
            }
        }
    }
}

