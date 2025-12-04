using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Auth.Services;

/// <summary>
/// Service for syncing notification preferences from Auth service to Notification service
/// </summary>
public interface INotificationPreferencesSyncService
{
    /// <summary>
    /// Syncs notification preferences to Notification service
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="preferencesJson">Preferences JSON containing notification preferences</param>
    /// <returns>True if sync was successful, false otherwise</returns>
    Task<bool> SyncNotificationPreferencesAsync(Guid userId, string preferencesJson);
}

public class NotificationPreferencesSyncService : INotificationPreferencesSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationPreferencesSyncService> _logger;
    private readonly string? _notificationServiceUrl;

    public NotificationPreferencesSyncService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NotificationPreferencesSyncService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        // Get Notification service URL from configuration
        // In production, this should be the ALB URL or service discovery URL
        _notificationServiceUrl = _configuration["NotificationService:BaseUrl"] 
            ?? Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL")
            ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com";
        
        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<bool> SyncNotificationPreferencesAsync(Guid userId, string preferencesJson)
    {
        try
        {
            // Parse preferences JSON to extract notification preferences
            var preferencesDoc = JsonDocument.Parse(preferencesJson);
            var root = preferencesDoc.RootElement;

            // Check if notification preferences exist
            if (!root.TryGetProperty("notifications", out var notificationsElement) &&
                !root.TryGetProperty("notificationPreferences", out notificationsElement))
            {
                // No notification preferences to sync
                return true;
            }

            var notificationPrefs = notificationsElement;

            // Map notification preferences to Notification service format
            // Notification service expects channel-based preferences
            var channelsToSync = new List<string> { "Email", "SMS", "WebPush", "Telegram" };
            var channelPreferences = new List<object>();

            foreach (var channel in channelsToSync)
            {
                var channelEnabled = GetChannelEnabled(notificationPrefs, channel);
                var notificationTypes = GetNotificationTypes(notificationPrefs, channel);
                var quietHours = GetQuietHours(notificationPrefs);

                // Only include channels with actual preferences
                if (channelEnabled.HasValue || notificationTypes != null || quietHours != null)
                {
                    channelPreferences.Add(new
                    {
                        channel = channel,
                        enabled = channelEnabled,
                        notificationTypes = notificationTypes,
                        quietHoursStart = quietHours?.Start,
                        quietHoursEnd = quietHours?.End
                    });
                }
            }

            // Only sync if there are preferences to sync
            if (channelPreferences.Count == 0)
            {
                return true; // No notification preferences to sync
            }

            // Prepare sync request for internal endpoint
            var syncRequest = new
            {
                userId = userId,
                channelPreferences = channelPreferences
            };

            var jsonContent = JsonSerializer.Serialize(syncRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Use internal sync endpoint (doesn't require user authentication)
            var requestUrl = $"{_notificationServiceUrl}/api/v1/notifications/preferences/channels/sync";
            
            try
            {
                // Add service-to-service authentication if configured
                var serviceApiKey = _configuration["ServiceAuth:ApiKey"] 
                    ?? Environment.GetEnvironmentVariable("SERVICE_AUTH_API_KEY");
                
                if (!string.IsNullOrEmpty(serviceApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-Service-Api-Key");
                    _httpClient.DefaultRequestHeaders.Add("X-Service-Api-Key", serviceApiKey);
                }

                var response = await _httpClient.PutAsync(requestUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Successfully synced notification preferences for user {UserId}",
                        userId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Failed to sync notification preferences for user {UserId}. Status: {Status}, Error: {Error}",
                        userId, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error syncing notification preferences for user {UserId}",
                    userId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error syncing notification preferences for user {UserId}. Preferences will remain in Auth service only.",
                userId);
            // Fail-open: Don't throw, just log the error
            // Preferences are saved in Auth service, sync failure doesn't block the update
            return false;
        }
    }

    private bool? GetChannelEnabled(JsonElement notificationPrefs, string channel)
    {
        return channel.ToLower() switch
        {
            "email" => notificationPrefs.TryGetProperty("emailNotifications", out var email) 
                ? email.GetBoolean() : null,
            "sms" => notificationPrefs.TryGetProperty("smsNotifications", out var sms) 
                ? sms.GetBoolean() : null,
            "webpush" => notificationPrefs.TryGetProperty("browserNotifications", out var browser) 
                ? browser.GetBoolean() : null,
            "telegram" => notificationPrefs.TryGetProperty("telegramNotifications", out var telegram) 
                ? telegram.GetBoolean() : null,
            _ => null
        };
    }

    private List<string>? GetNotificationTypes(JsonElement notificationPrefs, string channel)
    {
        var types = new List<string>();
        
        if (notificationPrefs.TryGetProperty("lotteryResults", out var lotteryResults) && lotteryResults.GetBoolean())
            types.Add("lottery_results");
        if (notificationPrefs.TryGetProperty("newLotteries", out var newLotteries) && newLotteries.GetBoolean())
            types.Add("new_lotteries");
        if (notificationPrefs.TryGetProperty("promotions", out var promotions) && promotions.GetBoolean())
            types.Add("promotions");
        if (notificationPrefs.TryGetProperty("accountUpdates", out var accountUpdates) && accountUpdates.GetBoolean())
            types.Add("account_updates");
        if (notificationPrefs.TryGetProperty("securityAlerts", out var securityAlerts) && securityAlerts.GetBoolean())
            types.Add("security_alerts");

        return types.Count > 0 ? types : null;
    }

    private (TimeSpan? Start, TimeSpan? End)? GetQuietHours(JsonElement notificationPrefs)
    {
        if (!notificationPrefs.TryGetProperty("quietHours", out var quietHours))
            return null;

        if (quietHours.ValueKind != JsonValueKind.Object)
            return null;

        if (!quietHours.TryGetProperty("enabled", out var enabled) || !enabled.GetBoolean())
            return null;

        TimeSpan? start = null;
        TimeSpan? end = null;

        if (quietHours.TryGetProperty("startTime", out var startTime))
        {
            if (TimeSpan.TryParse(startTime.GetString(), out var startSpan))
                start = startSpan;
        }

        if (quietHours.TryGetProperty("endTime", out var endTime))
        {
            if (TimeSpan.TryParse(endTime.GetString(), out var endSpan))
                end = endSpan;
        }

        return (start, end);
    }
}

