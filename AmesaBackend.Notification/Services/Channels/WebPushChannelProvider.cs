using WebPush;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AmesaBackend.Notification.Services.Channels
{
    public class WebPushChannelProvider : IChannelProvider
    {
        public string ChannelName => NotificationChannelConstants.WebPush;

        private readonly IConfiguration _configuration;
        private readonly ILogger<WebPushChannelProvider> _logger;
        private readonly NotificationDbContext _context;
        private readonly VapidDetails _vapidDetails;

        public WebPushChannelProvider(
            IConfiguration configuration,
            ILogger<WebPushChannelProvider> logger,
            NotificationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;

            var webPushConfig = _configuration.GetSection("NotificationChannels:WebPush");
            var publicKey = webPushConfig["VapidPublicKey"] ?? "";
            var privateKey = webPushConfig["VapidPrivateKey"] ?? "";
            var subject = webPushConfig["VapidSubject"] ?? "mailto:noreply@amesa.com";

            // Validate VAPID keys
            if (string.IsNullOrEmpty(publicKey) || publicKey == "FROM_SECRETS")
            {
                _logger.LogWarning("VAPID public key is not configured. WebPush notifications will fail.");
            }
            if (string.IsNullOrEmpty(privateKey) || privateKey == "FROM_SECRETS")
            {
                _logger.LogWarning("VAPID private key is not configured. WebPush notifications will fail.");
            }

            _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        }

        public async Task<DeliveryResult> SendAsync(NotificationRequest request)
        {
            try
            {
                // Get all push subscriptions for the user
                var subscriptions = await _context.PushSubscriptions
                    .Where(s => s.UserId == request.UserId)
                    .ToListAsync();

                if (subscriptions.Count == 0)
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "No push subscriptions found for user"
                    };
                }

                int successCount = 0;
                int failureCount = 0;
                string? lastError = null;

                // Send to all subscriptions
                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        var pushSubscription = new PushSubscription(
                            subscription.Endpoint,
                            subscription.P256dhKey,
                            subscription.AuthKey);

                        var payload = JsonSerializer.Serialize(new
                        {
                            title = request.Title,
                            body = request.Message,
                            icon = "/assets/icons/icon-192x192.png",
                            badge = "/assets/icons/badge-72x72.png",
                            data = request.Data,
                            tag = request.Type,
                            requireInteraction = false
                        });

                        var webPushClient = new WebPushClient();
                        await webPushClient.SendNotificationAsync(pushSubscription, payload, _vapidDetails);

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        lastError = ex.Message;
                        _logger.LogWarning(ex, "Failed to send push notification to subscription {SubscriptionId}", subscription.Id);

                        // If subscription is invalid, remove it
                        if (ex.Message.Contains("410") || ex.Message.Contains("Gone"))
                        {
                            _context.PushSubscriptions.Remove(subscription);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (successCount > 0)
                {
                    return new DeliveryResult
                    {
                        Success = true,
                        ExternalId = $"{successCount}/{subscriptions.Count}",
                        Cost = 0m // Web Push is free
                    };
                }
                else
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = lastError ?? "All push notifications failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending web push notification for user {UserId}", request.UserId);
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<bool> ValidatePreferencesAsync(Guid userId, NotificationPreferences preferences)
        {
            // WebPush channel requires subscription
            return Task.FromResult(true);
        }

        public bool IsChannelEnabled(Guid userId)
        {
            // Check user channel preferences and subscription existence
            var preference = _context.UserChannelPreferences
                .FirstOrDefault(p => p.UserId == userId && p.Channel == NotificationChannelConstants.WebPush);
            
            if (preference != null && !preference.Enabled)
            {
                return false;
            }

            // Check if user has at least one subscription
            return _context.PushSubscriptions.Any(s => s.UserId == userId);
        }
    }
}

