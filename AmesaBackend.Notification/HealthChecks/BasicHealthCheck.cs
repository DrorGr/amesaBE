using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

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
            // #region agent log
            Log.Information("[DEBUG] BasicHealthCheck entry - sessionId=debug-session runId=run1 hypothesisId=B,E location=BasicHealthCheck.cs:11 checkName={CheckName}", context.Registration.Name);
            // #endregion
            // Basic health check - service is running if this can execute
            var result = HealthCheckResult.Healthy("Notification service is operational");
            // #region agent log
            Log.Information("[DEBUG] BasicHealthCheck exit - sessionId=debug-session runId=run1 hypothesisId=B,E location=BasicHealthCheck.cs:16 status={Status} description={Description}", result.Status, result.Description);
            // #endregion
            return Task.FromResult(result);
        }
    }
}

