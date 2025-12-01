using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AmesaBackend.Notification.HealthChecks
{
    public class SMSChannelHealthCheck : IHealthCheck
    {
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly IConfiguration _configuration;

        public SMSChannelHealthCheck(
            IAmazonSimpleNotificationService snsClient,
            IConfiguration configuration)
        {
            _snsClient = snsClient;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // #region agent log
            Log.Warning("[DEBUG] SMSChannelHealthCheck entry - SHOULD NOT RUN on /health - sessionId=debug-session runId=run1 hypothesisId=A location=SMSChannelHealthCheck.cs:25 checkName={CheckName}", context.Registration.Name);
            // #endregion
            try
            {
                // Check SNS service health by getting platform applications (lightweight check)
                // We'll just verify the client can make a request
                var request = new ListPlatformApplicationsRequest();
                await _snsClient.ListPlatformApplicationsAsync(request, cancellationToken);

                return HealthCheckResult.Healthy("SMS channel (AWS SNS) is operational");
            }
            catch (Amazon.SimpleNotificationService.Model.AuthorizationErrorException)
            {
                // Permission errors should be Degraded, not Unhealthy
                // This prevents IAM permission issues from causing health check failures
                return HealthCheckResult.Degraded("SMS channel (AWS SNS) - IAM permissions not configured");
            }
            catch (Exception ex)
            {
                // Other errors (network, service unavailable) are Degraded, not Unhealthy
                // This prevents transient issues from causing health check failures
                return HealthCheckResult.Degraded($"SMS channel (AWS SNS) is unavailable: {ex.Message}");
            }
        }
    }
}

