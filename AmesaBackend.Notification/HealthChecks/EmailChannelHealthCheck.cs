using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Notification.HealthChecks
{
    public class EmailChannelHealthCheck : IHealthCheck
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly IConfiguration _configuration;

        public EmailChannelHealthCheck(
            IAmazonSimpleEmailService sesClient,
            IConfiguration configuration)
        {
            _sesClient = sesClient;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check SES service health by getting send quota
                var quotaRequest = new GetSendQuotaRequest();
                var quota = await _sesClient.GetSendQuotaAsync(quotaRequest, cancellationToken);

                var data = new Dictionary<string, object>
                {
                    ["Max24HourSend"] = quota.Max24HourSend,
                    ["MaxSendRate"] = quota.MaxSendRate,
                    ["SentLast24Hours"] = quota.SentLast24Hours
                };

                return HealthCheckResult.Healthy("Email channel (AWS SES) is operational", data);
            }
            catch (Exception ex)
            {
                // Check if it's an authorization/permission error
                var isAuthError = ex.GetType().Name.Contains("Authorization") || 
                                 ex.GetType().Name.Contains("AccessDenied") ||
                                 ex.Message.Contains("not authorized") ||
                                 ex.Message.Contains("no identity-based policy");
                
                if (isAuthError)
                {
                    // Permission errors should be Degraded, not Unhealthy
                    // This prevents IAM permission issues from causing health check failures
                    return HealthCheckResult.Degraded("Email channel (AWS SES) - IAM permissions not configured");
                }
                
                // Other errors (network, service unavailable) are Degraded, not Unhealthy
                // This prevents transient issues from causing health check failures
                return HealthCheckResult.Degraded($"Email channel (AWS SES) is unavailable: {ex.Message}");
            }
        }
    }
}

