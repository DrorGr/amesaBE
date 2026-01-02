using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using AmesaBackend.Notification.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;

namespace AmesaBackend.Notification.Services
{
    public interface INotificationPreferenceService
    {
        Task<bool> ShouldSendNotificationAsync(Guid userId, string notificationTypeCode, string channel);
        Task<UserFeaturePreference?> GetFeaturePreferenceAsync(Guid userId, string feature);
        Task<UserTypePreference?> GetTypePreferenceAsync(Guid userId, string notificationTypeCode);
        Task<List<UserFeaturePreference>> GetFeaturePreferencesAsync(Guid userId);
        Task<List<UserTypePreference>> GetTypePreferencesAsync(Guid userId);
        Task UpdateFeaturePreferenceAsync(Guid userId, string feature, bool enabled, string[]? channels = null, TimeSpan? quietHoursStart = null, TimeSpan? quietHoursEnd = null, int? frequencyLimit = null, string? frequencyWindow = null);
        Task UpdateTypePreferenceAsync(Guid userId, string notificationTypeCode, bool enabled, string[]? channels = null, int? priority = null);
        Task InitializeDefaultPreferencesAsync(Guid userId);
    }
    
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private readonly NotificationDbContext _context;
        private readonly INotificationTypeMappingService _typeMapping;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<NotificationPreferenceService> _logger;
        
        public NotificationPreferenceService(
            NotificationDbContext context,
            INotificationTypeMappingService typeMapping,
            IRateLimitService rateLimitService,
            ILogger<NotificationPreferenceService> logger)
        {
            _context = context;
            _typeMapping = typeMapping;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }
        
        public async Task<bool> ShouldSendNotificationAsync(
            Guid userId,
            string notificationTypeCode,
            string channel)
        {
            try
            {
                // 1. Check if notification type exists
                if (!await _typeMapping.TypeExistsAsync(notificationTypeCode))
                {
                    _logger.LogWarning("Notification type {TypeCode} does not exist", notificationTypeCode);
                    return false;
                }
                
                // 2. Check if type is critical (cannot be disabled)
                var isCritical = await _typeMapping.IsCriticalAsync(notificationTypeCode);
                
                // 3. Get feature for this notification type
                var feature = await _typeMapping.GetFeatureAsync(notificationTypeCode);
                
                // 4. Check feature preference
                var featurePref = await GetFeaturePreferenceAsync(userId, feature);
                if (featurePref != null && !featurePref.Enabled && !isCritical)
                {
                    _logger.LogDebug("Feature {Feature} disabled for user {UserId}", feature, userId);
                    return false;
                }
                
                // 5. Check if channel is enabled for feature
                if (featurePref != null && featurePref.Channels.Length > 0 && !featurePref.Channels.Contains(channel) && !isCritical)
                {
                    _logger.LogDebug("Channel {Channel} not enabled for feature {Feature}", channel, feature);
                    return false;
                }
                
                // 6. Check type-specific preference
                var typePref = await GetTypePreferenceAsync(userId, notificationTypeCode);
                if (typePref != null && !typePref.Enabled && !isCritical)
                {
                    _logger.LogDebug("Notification type {TypeCode} disabled for user {UserId}", notificationTypeCode, userId);
                    return false;
                }
                
                // 7. Check if channel is enabled for type
                if (typePref != null && typePref.Channels.Length > 0 && !typePref.Channels.Contains(channel) && !isCritical)
                {
                    _logger.LogDebug("Channel {Channel} not enabled for type {TypeCode}", channel, notificationTypeCode);
                    return false;
                }
                
                // 8. Check channel preference (legacy support)
                var channelPref = await _context.UserChannelPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Channel == channel);
                
                if (channelPref != null && !channelPref.Enabled && !isCritical)
                {
                    _logger.LogDebug("Channel {Channel} disabled for user {UserId}", channel, userId);
                    return false;
                }
                
                // 9. Check notification types in channel preference
                if (channelPref != null && !string.IsNullOrEmpty(channelPref.NotificationTypes))
                {
                    var enabledTypes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(channelPref.NotificationTypes);
                    if (enabledTypes != null && enabledTypes.Count > 0 && !enabledTypes.Contains(notificationTypeCode) && !isCritical)
                    {
                        _logger.LogDebug("Notification type {TypeCode} not in channel {Channel} enabled types", notificationTypeCode, channel);
                        return false;
                    }
                }
                
                // 10. Check quiet hours (handle midnight crossover)
                if (featurePref != null && featurePref.QuietHoursStart.HasValue && featurePref.QuietHoursEnd.HasValue)
                {
                    var now = DateTime.UtcNow.TimeOfDay;
                    var start = featurePref.QuietHoursStart.Value;
                    var end = featurePref.QuietHoursEnd.Value;
                    
                    bool inQuietHours;
                    if (start <= end)
                    {
                        // Normal case: quiet hours don't cross midnight (e.g., 22:00-06:00)
                        inQuietHours = now >= start && now <= end;
                    }
                    else
                    {
                        // Midnight crossover case: quiet hours cross midnight (e.g., 22:00-02:00)
                        inQuietHours = now >= start || now <= end;
                    }
                    
                    if (inQuietHours)
                    {
                        _logger.LogDebug("Quiet hours active for feature {Feature} (start: {Start}, end: {End}, now: {Now})", 
                            feature, start, end, now);
                        return false;
                    }
                }
                
                // 11. Check frequency limits
                if (featurePref != null && featurePref.FrequencyLimit.HasValue)
                {
                    var window = featurePref.FrequencyWindow ?? "hour";
                    var limitKey = $"notification_frequency:{userId}:{feature}:{window}";
                    var canSend = await _rateLimitService.CheckRateLimitAsync(
                        limitKey, 
                        featurePref.FrequencyLimit.Value, 
                        window == "hour" ? TimeSpan.FromHours(1) : TimeSpan.FromDays(1));
                    
                    if (!canSend)
                    {
                        _logger.LogDebug("Frequency limit exceeded for feature {Feature}", feature);
                        return false;
                    }
                }
                
                // 12. Check channel rate limits (existing logic)
                var channelRateLimitKey = $"{NotificationChannelConstants.RateLimitPrefix}{channel}:{userId}";
                var channelRateLimits = GetRateLimitsForChannel(channel);
                var canSendChannel = await _rateLimitService.CheckRateLimitAsync(
                    channelRateLimitKey, 
                    channelRateLimits.limit, 
                    channelRateLimits.window);
                
                if (!canSendChannel)
                {
                    _logger.LogDebug("Channel rate limit exceeded for {Channel}", channel);
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking notification preferences for user {UserId}, type {TypeCode}, channel {Channel}", 
                    userId, notificationTypeCode, channel);
                
                // Fail-closed for critical notifications (security, payment, account)
                // Fail-open for non-critical notifications (availability over preference enforcement)
                var isCritical = await _typeMapping.IsCriticalAsync(notificationTypeCode);
                if (isCritical)
                {
                    _logger.LogWarning("Preference check failed for critical notification type {TypeCode}. Failing closed (not sending).", 
                        notificationTypeCode);
                    return false;
                }
                
                // Fail open for non-critical notifications
                _logger.LogWarning("Preference check failed for non-critical notification. Failing open (sending notification).");
                return true;
            }
        }
        
        public async Task<UserFeaturePreference?> GetFeaturePreferenceAsync(Guid userId, string feature)
        {
            return await _context.UserFeaturePreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Feature == feature);
        }
        
        public async Task<UserTypePreference?> GetTypePreferenceAsync(Guid userId, string notificationTypeCode)
        {
            return await _context.UserTypePreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationTypeCode == notificationTypeCode);
        }
        
        public async Task<List<UserFeaturePreference>> GetFeaturePreferencesAsync(Guid userId)
        {
            return await _context.UserFeaturePreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }
        
        public async Task<List<UserTypePreference>> GetTypePreferencesAsync(Guid userId)
        {
            return await _context.UserTypePreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }
        
        public async Task UpdateFeaturePreferenceAsync(
            Guid userId, 
            string feature, 
            bool enabled, 
            string[]? channels = null, 
            TimeSpan? quietHoursStart = null, 
            TimeSpan? quietHoursEnd = null, 
            int? frequencyLimit = null, 
            string? frequencyWindow = null)
        {
            var pref = await GetFeaturePreferenceAsync(userId, feature);
            
            if (pref == null)
            {
                pref = new UserFeaturePreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Feature = feature,
                    Enabled = enabled,
                    Channels = channels ?? Array.Empty<string>(),
                    QuietHoursStart = quietHoursStart,
                    QuietHoursEnd = quietHoursEnd,
                    FrequencyLimit = frequencyLimit,
                    FrequencyWindow = frequencyWindow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserFeaturePreferences.Add(pref);
            }
            else
            {
                pref.Enabled = enabled;
                if (channels != null)
                {
                    pref.Channels = channels;
                }
                if (quietHoursStart.HasValue)
                {
                    pref.QuietHoursStart = quietHoursStart;
                }
                if (quietHoursEnd.HasValue)
                {
                    pref.QuietHoursEnd = quietHoursEnd;
                }
                if (frequencyLimit.HasValue)
                {
                    pref.FrequencyLimit = frequencyLimit;
                }
                if (!string.IsNullOrEmpty(frequencyWindow))
                {
                    pref.FrequencyWindow = frequencyWindow;
                }
                pref.UpdatedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
        }
        
        public async Task UpdateTypePreferenceAsync(
            Guid userId, 
            string notificationTypeCode, 
            bool enabled, 
            string[]? channels = null, 
            int? priority = null)
        {
            var pref = await GetTypePreferenceAsync(userId, notificationTypeCode);
            
            if (pref == null)
            {
                pref = new UserTypePreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NotificationTypeCode = notificationTypeCode,
                    Enabled = enabled,
                    Channels = channels ?? Array.Empty<string>(),
                    Priority = priority ?? 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserTypePreferences.Add(pref);
            }
            else
            {
                pref.Enabled = enabled;
                if (channels != null)
                {
                    pref.Channels = channels;
                }
                if (priority.HasValue)
                {
                    pref.Priority = priority.Value;
                }
                pref.UpdatedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
        }
        
        public async Task InitializeDefaultPreferencesAsync(Guid userId)
        {
            // Initialize default feature preferences
            var features = new[] { "authentication", "security", "lottery", "payment", "profile", "system" };
            
            foreach (var feature in features)
            {
                var existing = await GetFeaturePreferenceAsync(userId, feature);
                if (existing == null)
                {
                    var defaultChannels = feature switch
                    {
                        "security" => new[] { "email", "sms" },
                        "lottery" => new[] { "email", "webpush" },
                        "payment" => new[] { "email", "sms" },
                        _ => new[] { "email" }
                    };
                    
                    await UpdateFeaturePreferenceAsync(userId, feature, true, defaultChannels);
                }
            }
            
            _logger.LogInformation("Initialized default preferences for user {UserId}", userId);
        }
        
        private (int limit, TimeSpan window) GetRateLimitsForChannel(string channel)
        {
            return channel.ToLower() switch
            {
                NotificationChannelConstants.Email => (10, TimeSpan.FromHours(1)),
                NotificationChannelConstants.SMS => (5, TimeSpan.FromHours(1)),
                NotificationChannelConstants.WebPush => (20, TimeSpan.FromHours(1)),
                NotificationChannelConstants.Telegram => (10, TimeSpan.FromHours(1)),
                _ => (10, TimeSpan.FromHours(1))
            };
        }
    }
}

