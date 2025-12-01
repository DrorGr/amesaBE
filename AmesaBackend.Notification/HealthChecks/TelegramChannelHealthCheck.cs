using Telegram.Bot;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Notification.HealthChecks
{
    public class TelegramChannelHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramChannelHealthCheck> _logger;

        public TelegramChannelHealthCheck(
            IConfiguration configuration,
            ILogger<TelegramChannelHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var botToken = _configuration["NotificationChannels:Telegram:BotToken"];
                
                if (string.IsNullOrEmpty(botToken) || botToken == "FROM_SECRETS")
                {
                    return HealthCheckResult.Degraded("Telegram bot token not configured");
                }

                var botClient = new TelegramBotClient(botToken);
                var me = await botClient.GetMeAsync(cancellationToken);

                var data = new Dictionary<string, object>
                {
                    ["BotUsername"] = me.Username ?? "unknown",
                    ["BotId"] = me.Id
                };

                return HealthCheckResult.Healthy("Telegram channel is operational", data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram health check failed");
                // Return Degraded instead of Unhealthy to prevent health check failures
                // Telegram API issues shouldn't cause service to be marked unhealthy
                return HealthCheckResult.Degraded($"Telegram channel is unavailable: {ex.Message}");
            }
        }
    }
}

