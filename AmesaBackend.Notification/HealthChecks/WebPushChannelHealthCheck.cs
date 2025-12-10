using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Notification.HealthChecks
{
    public class WebPushChannelHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;

        public WebPushChannelHealthCheck(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var webPushConfig = _configuration.GetSection("NotificationChannels:WebPush");
                var publicKey = webPushConfig["VapidPublicKey"] ?? "";
                var privateKey = webPushConfig["VapidPrivateKey"] ?? "";

                if (string.IsNullOrEmpty(publicKey) || publicKey == "FROM_SECRETS")
                {
                    return Task.FromResult(HealthCheckResult.Degraded("WebPush VAPID public key not configured"));
                }

                if (string.IsNullOrEmpty(privateKey) || privateKey == "FROM_SECRETS")
                {
                    return Task.FromResult(HealthCheckResult.Degraded("WebPush VAPID private key not configured"));
                }

                // Validate key format (VAPID keys are base64 URL-safe strings, typically 87-88 characters)
                if (publicKey.Length < 80 || publicKey.Length > 100)
                {
                    return Task.FromResult(HealthCheckResult.Degraded("WebPush VAPID public key format appears invalid"));
                }

                return Task.FromResult(HealthCheckResult.Healthy("WebPush channel is operational"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("WebPush channel check failed", ex));
            }
        }
    }
}












