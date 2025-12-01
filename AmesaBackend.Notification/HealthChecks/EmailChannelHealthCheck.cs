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
                return HealthCheckResult.Unhealthy("Email channel (AWS SES) is unavailable", ex);
            }
        }
    }
}

