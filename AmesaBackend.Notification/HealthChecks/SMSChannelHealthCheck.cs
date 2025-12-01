using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

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
            try
            {
                // Check SNS service health by getting platform applications (lightweight check)
                // We'll just verify the client can make a request
                var request = new ListPlatformApplicationsRequest();
                await _snsClient.ListPlatformApplicationsAsync(request, cancellationToken);

                return HealthCheckResult.Healthy("SMS channel (AWS SNS) is operational");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("SMS channel (AWS SNS) is unavailable", ex);
            }
        }
    }
}

