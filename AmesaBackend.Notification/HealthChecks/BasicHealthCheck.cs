using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AmesaBackend.Notification.HealthChecks
{
    /// <summary>
    /// Basic health check that only verifies the service is running.
    /// Used for ALB health checks to avoid false negatives from channel-specific checks.
    /// </summary>
    public class BasicHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Basic health check - service is running if this can execute
            return Task.FromResult(HealthCheckResult.Healthy("Notification service is operational"));
        }
    }
}

