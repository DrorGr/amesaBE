using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Models;
using AmesaBackend.Notification.Hubs;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Auth.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Npgsql;

namespace AmesaBackend.Notification.Services
{
    public class NotificationOrchestrator : INotificationOrchestrator
    {
        private readonly NotificationDbContext _context;
        private readonly IEnumerable<IChannelProvider> _channelProviders;
        private readonly ITemplateEngine _templateEngine;
        private readonly ICache _cache;
        private readonly IEventPublisher _eventPublisher;
        private readonly IHttpRequest _httpRequest;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationOrchestrator> _logger;
        private readonly IHubContext<NotificationHub>? _hubContext;
        private readonly IRateLimitService _rateLimitService;
        private readonly INotificationPreferenceService _preferenceService;
        private readonly ICloudWatchMetricsService? _metricsService;

        public NotificationOrchestrator(
            NotificationDbContext context,
            IEnumerable<IChannelProvider> channelProviders,
            ITemplateEngine templateEngine,
            ICache cache,
            IEventPublisher eventPublisher,
            IHttpRequest httpRequest,
            IConfiguration configuration,
            ILogger<NotificationOrchestrator> logger,
            IRateLimitService rateLimitService,
            INotificationPreferenceService preferenceService,
            IHubContext<NotificationHub>? hubContext = null,
            ICloudWatchMetricsService? metricsService = null)
        {
            _context = context;
            _channelProviders = channelProviders;
            _templateEngine = templateEngine;
            _cache = cache;
            _eventPublisher = eventPublisher;
            _httpRequest = httpRequest;
            _configuration = configuration;
            _logger = logger;
            _rateLimitService = rateLimitService;
            _preferenceService = preferenceService;
            _hubContext = hubContext;
            _metricsService = metricsService;
        }

        public async Task<OrchestrationResult> SendMultiChannelAsync(Guid userId, NotificationRequest request, List<string> channels)
        {
            var result = new OrchestrationResult
            {
                NotificationId = Guid.NewGuid()
            };

            // Use explicit transaction for data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Set language from user preferences if not provided
                if (string.IsNullOrEmpty(request.Language))
                {
                    // Use default language - user language preference can be retrieved from user preferences service if needed
                    request.Language = NotificationChannelConstants.DefaultLanguage;
                }
                // Get user channel preferences from cache or database with error handling
                var cacheKey = $"{NotificationChannelConstants.CacheKeyUserPrefs}{userId}";
                Dictionary<string, object>? cachedPrefs = null;

                try
                {
                    cachedPrefs = await _cache.GetRecordAsync<Dictionary<string, object>>(cacheKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis cache unavailable for user {UserId}, falling back to database", userId);
                    // Continue with null cachedPrefs - will fetch from database
                }
                
                // If not in cache, fetch from database
                if (cachedPrefs == null)
                {
                    var preferences = await _context.UserChannelPreferences
                        .Where(p => p.UserId == userId)
                        .ToListAsync();
                    
                    cachedPrefs = new Dictionary<string, object>();
                    foreach (var pref in preferences)
                    {
                        if (pref.QuietHoursStart.HasValue && pref.QuietHoursEnd.HasValue)
                        {
                            cachedPrefs["quietHoursStart"] = pref.QuietHoursStart.Value.ToString(@"hh\:mm\:ss");
                            cachedPrefs["quietHoursEnd"] = pref.QuietHoursEnd.Value.ToString(@"hh\:mm\:ss");
                        }
                    }
                    
                    // Cache for 15 minutes (only if cache is available)
                    if (cachedPrefs.Count > 0)
                    {
                        try
                        {
                            await _cache.SetRecordAsync(cacheKey, cachedPrefs, TimeSpan.FromMinutes(15));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to cache preferences for user {UserId}, continuing without cache", userId);
                            // Continue - cache is optional
                        }
                    }
                }
                
                // Check quiet hours if configured
                if (cachedPrefs.ContainsKey("quietHoursStart") && cachedPrefs.ContainsKey("quietHoursEnd"))
                {
                    var now = DateTime.UtcNow.TimeOfDay;
                    if (TimeSpan.TryParse(cachedPrefs["quietHoursStart"]?.ToString(), out var start) &&
                        TimeSpan.TryParse(cachedPrefs["quietHoursEnd"]?.ToString(), out var end))
                    {
                        if (now >= start && now <= end)
                        {
                            _logger.LogInformation("Quiet hours active for user {UserId}, skipping notification", userId);
                            return result;
                        }
                    }
                }

                // Create notification record
                var notification = new UserNotification
                {
                    Id = result.NotificationId,
                    UserId = userId,
                    Type = request.Type,
                    NotificationTypeCode = request.Type, // Use Type as NotificationTypeCode if not specified in request
                    Title = request.Title,
                    Message = request.Message,
                    Data = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserNotifications.Add(notification);

                // Save notification record first to ensure data consistency
                // If SaveChangesAsync fails, no notifications will be sent
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict saving notification {NotificationId}", result.NotificationId);
                    throw; // Let retry logic handle
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database update failed for notification {NotificationId}", result.NotificationId);
                    throw;
                }
                catch (NpgsqlException ex) when (ex.IsTransient)
                {
                    _logger.LogWarning(ex, "Transient database error saving notification {NotificationId}, will retry", result.NotificationId);
                    throw; // Retry policy should handle
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Database error saving notification {NotificationId}: {Error}", 
                        result.NotificationId, ex.Message);
                    throw;
                }

                // Process each channel - prepare delivery records (don't send yet)
                var notificationTypeCode = notification.NotificationTypeCode ?? request.Type;
                var deliveriesToAdd = new List<NotificationDelivery>();
                var channelsToSend = new List<(string ChannelName, IChannelProvider Provider, NotificationDelivery Delivery)>();
                
                // Check per-user rate limit across all channels
                var userRateLimitKey = $"notification_user:{userId}";
                var userRateLimit = 50; // Max 50 notifications per hour per user
                var userCanSend = await _rateLimitService.CheckRateLimitAsync(
                    userRateLimitKey, 
                    userRateLimit, 
                    TimeSpan.FromHours(1));

                if (!userCanSend)
                {
                    _logger.LogWarning("Per-user rate limit exceeded for user {UserId}", userId);
                    await transaction.RollbackAsync();
                    result.DeliveryResults.Add(new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "User rate limit exceeded"
                    });
                    result.FailureCount++;
                    return result;
                }
                
                foreach (var channelName in channels)
                {
                    var provider = _channelProviders.FirstOrDefault(p => p.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase));
                    if (provider == null)
                    {
                        _logger.LogWarning("Channel provider not found: {ChannelName}", channelName);
                        result.DeliveryResults.Add(new DeliveryResult
                        {
                            Success = false,
                            ErrorMessage = $"Channel provider not found: {channelName}"
                        });
                        result.FailureCount++;
                        continue;
                    }

                    // Check if notification should be sent based on user preferences (feature, type, channel, quiet hours, frequency limits)
                    if (!await _preferenceService.ShouldSendNotificationAsync(userId, notificationTypeCode, channelName))
                    {
                        _logger.LogInformation("Notification {Type} not allowed for user {UserId} on channel {Channel}", 
                            notificationTypeCode, userId, channelName);
                        continue;
                    }

                    // Check if channel provider is available and enabled
                    if (!provider.IsChannelEnabled(userId))
                    {
                        _logger.LogInformation("Channel {ChannelName} is disabled for user {UserId}", channelName, userId);
                        continue;
                    }

                    // Check rate limiting per channel using shared IRateLimitService
                    var rateLimitKey = $"{NotificationChannelConstants.RateLimitPrefix}{channelName}:{userId}";
                    var rateLimits = GetRateLimitsForChannel(channelName);
                    var canSend = await _rateLimitService.CheckRateLimitAsync(rateLimitKey, rateLimits.limit, rateLimits.window);
                    if (!canSend)
                    {
                        _logger.LogWarning("Rate limit exceeded for channel {ChannelName} and user {UserId}", channelName, userId);
                        result.DeliveryResults.Add(new DeliveryResult
                        {
                            Success = false,
                            ErrorMessage = "Rate limit exceeded"
                        });
                        result.FailureCount++;
                        continue;
                    }

                    // Create delivery record (will be updated after send)
                    var delivery = new NotificationDelivery
                    {
                        Id = Guid.NewGuid(),
                        NotificationId = result.NotificationId,
                        Channel = channelName,
                        Status = NotificationChannelConstants.StatusPending,
                        Currency = NotificationChannelConstants.DefaultCurrency,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    deliveriesToAdd.Add(delivery);
                    channelsToSend.Add((channelName, provider, delivery));
                }

                // Save all delivery records in same transaction
                if (deliveriesToAdd.Count > 0)
                {
                    try
                    {
                        _context.NotificationDeliveries.AddRange(deliveriesToAdd);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        _logger.LogError(ex, "Failed to save delivery records for notification {NotificationId}", result.NotificationId);
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                
                // Commit transaction before sending notifications
                await transaction.CommitAsync();
                
                // Increment per-user rate limit after successful commit
                await _rateLimitService.IncrementRateLimitAsync(userRateLimitKey, TimeSpan.FromHours(1));
                
                // Now send notifications (outside transaction to avoid long-running transactions)
                foreach (var (channelName, provider, delivery) in channelsToSend)
                {
                    try
                    {
                        // Send notification via channel
                        var deliveryResult = await provider.SendAsync(request);

                        // Update delivery record
                        delivery.Status = deliveryResult.Success ? NotificationChannelConstants.StatusSent : NotificationChannelConstants.StatusFailed;
                        delivery.ExternalId = deliveryResult.ExternalId;
                        delivery.ErrorMessage = deliveryResult.ErrorMessage;
                        delivery.Cost = deliveryResult.Cost;
                        delivery.UpdatedAt = DateTime.UtcNow;

                        if (deliveryResult.Success)
                        {
                            delivery.DeliveredAt = DateTime.UtcNow;
                            result.SuccessCount++;
                            
                            // Increment rate limit only after successful send
                            var rateLimitKey = $"{NotificationChannelConstants.RateLimitPrefix}{channelName}:{userId}";
                            var rateLimits = GetRateLimitsForChannel(channelName);
                            await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, rateLimits.window);
                            
                            // Send CloudWatch metrics
                            if (_metricsService != null)
                            {
                                await _metricsService.PutMetricAsync("NotificationDeliverySuccess", 1, "Count");
                                await _metricsService.PutMetricWithDimensionsAsync("NotificationDeliveryByChannel", 
                                    new Dictionary<string, string> { { "Channel", channelName } }, 1, "Count");
                            }
                        }
                        else
                        {
                            result.FailureCount++;
                            
                            // Send CloudWatch metrics for failures
                            if (_metricsService != null)
                            {
                                await _metricsService.PutMetricAsync("NotificationDeliveryFailure", 1, "Count");
                                await _metricsService.PutMetricWithDimensionsAsync("NotificationDeliveryFailureByChannel", 
                                    new Dictionary<string, string> { { "Channel", channelName } }, 1, "Count");
                            }
                        }

                        result.DeliveryResults.Add(deliveryResult);
                        
                        // Update delivery in database
                        _context.NotificationDeliveries.Update(delivery);
                        await _context.SaveChangesAsync();

                        // Publish EventBridge event (non-blocking, but track failures)
                        try
                        {
                            var eventToPublish = new NotificationSentEvent
                            {
                                NotificationId = result.NotificationId,
                                UserId = userId,
                                Channel = channelName,
                                Type = request.Type,
                                Source = "amesa.notification-service",
                                DetailType = EventBridgeConstants.DetailType.NotificationSent
                            };
                            await _eventPublisher.PublishAsync(eventToPublish);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to publish EventBridge event for notification {NotificationId}. This may affect analytics.", result.NotificationId);
                            // Don't fail the notification delivery, but log for monitoring
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send notification via channel {Channel} for notification {NotificationId}", 
                            channelName, result.NotificationId);
                        
                        // Update delivery status to failed
                        delivery.Status = NotificationChannelConstants.StatusFailed;
                        delivery.ErrorMessage = ex.Message;
                        delivery.UpdatedAt = DateTime.UtcNow;
                        _context.NotificationDeliveries.Update(delivery);
                        await _context.SaveChangesAsync();
                        
                        result.FailureCount++;
                        result.DeliveryResults.Add(new DeliveryResult
                        {
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                // Send real-time notification via SignalR with retry mechanism
                if (_hubContext != null)
                {
                    const int maxRetries = 3;
                    var retryCount = 0;
                    var sent = false;
                    
                    while (!sent && retryCount < maxRetries)
                    {
                        try
                        {
                            var notificationDto = new NotificationDto
                            {
                                Id = notification.Id,
                                UserId = notification.UserId,
                                TemplateId = notification.TemplateId,
                                Type = notification.Type,
                                Title = notification.Title,
                                Message = notification.Message,
                                IsRead = notification.IsRead,
                                ReadAt = notification.ReadAt,
                                Data = notification.Data != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Data) : null,
                                CreatedAt = notification.CreatedAt
                            };

                            await _hubContext.Clients
                                .Group($"notifications_{userId}")
                                .SendAsync("ReceiveNotification", notificationDto);
                            
                            sent = true;
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            if (retryCount >= maxRetries)
                            {
                                _logger.LogWarning(ex, "Failed to send SignalR notification for user {UserId} after {Retries} retries", 
                                    userId, maxRetries);
                                result.SignalRFailed = true;
                                result.SignalRErrorMessage = ex.Message;
                            }
                            else
                            {
                                _logger.LogWarning(ex, "SignalR send failed for user {UserId}, retry {Retry}/{MaxRetries}", 
                                    userId, retryCount, maxRetries);
                                await Task.Delay(100 * retryCount); // Exponential backoff
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMultiChannelAsync for user {UserId}", userId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<DeliveryStatusDto>> GetDeliveryStatusAsync(Guid notificationId)
        {
            try
            {
                var deliveries = await _context.NotificationDeliveries
                    .Where(d => d.NotificationId == notificationId)
                    .Select(d => new DeliveryStatusDto
                    {
                        Id = d.Id,
                        NotificationId = d.NotificationId,
                        Channel = d.Channel,
                        Status = d.Status,
                        ExternalId = d.ExternalId,
                        ErrorMessage = d.ErrorMessage,
                        DeliveredAt = d.DeliveredAt,
                        OpenedAt = d.OpenedAt,
                        ClickedAt = d.ClickedAt,
                        RetryCount = d.RetryCount,
                        Cost = d.Cost,
                        Currency = d.Currency,
                        CreatedAt = d.CreatedAt
                    })
                    .ToListAsync();

                return deliveries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery status for notification {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task ResendFailedNotificationAsync(Guid deliveryId)
        {
            const int maxRetries = 5;
            const int baseDelayMs = 1000; // 1 second base delay
            
            try
            {
                var delivery = await _context.NotificationDeliveries
                    .Include(d => d.Notification)
                    .FirstOrDefaultAsync(d => d.Id == deliveryId);

                if (delivery == null || delivery.Notification == null)
                {
                    _logger.LogWarning("Delivery {DeliveryId} not found", deliveryId);
                    return;
                }

                if (delivery.Status != NotificationChannelConstants.StatusFailed)
                {
                    _logger.LogWarning("Delivery {DeliveryId} is not in failed status", deliveryId);
                    return;
                }

                // Check retry limit
                if (delivery.RetryCount >= maxRetries)
                {
                    _logger.LogWarning("Delivery {DeliveryId} has exceeded max retries ({MaxRetries}). Not retrying.", 
                        deliveryId, maxRetries);
                    return;
                }

                var provider = _channelProviders.FirstOrDefault(p => p.ChannelName.Equals(delivery.Channel, StringComparison.OrdinalIgnoreCase));
                if (provider == null)
                {
                    _logger.LogWarning("Channel provider not found: {Channel}", delivery.Channel);
                    return;
                }

                // Exponential backoff: wait before retry
                if (delivery.RetryCount > 0)
                {
                    var delayMs = baseDelayMs * (int)Math.Pow(2, delivery.RetryCount - 1);
                    _logger.LogInformation("Waiting {DelayMs}ms before retry {RetryCount} for delivery {DeliveryId}", 
                        delayMs, delivery.RetryCount + 1, deliveryId);
                    await Task.Delay(delayMs);
                }

                var request = new NotificationRequest
                {
                    UserId = delivery.Notification.UserId,
                    Channel = delivery.Channel,
                    Type = delivery.Notification.Type,
                    Title = delivery.Notification.Title,
                    Message = delivery.Notification.Message,
                    Data = delivery.Notification.Data != null 
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(delivery.Notification.Data) 
                        : null
                };

                var result = await provider.SendAsync(request);

                delivery.Status = result.Success ? NotificationChannelConstants.StatusSent : NotificationChannelConstants.StatusFailed;
                delivery.ExternalId = result.ExternalId;
                delivery.ErrorMessage = result.ErrorMessage;
                delivery.RetryCount++;
                delivery.UpdatedAt = DateTime.UtcNow;

                if (result.Success)
                {
                    delivery.DeliveredAt = DateTime.UtcNow;
                    _logger.LogInformation("Successfully resent notification delivery {DeliveryId} on retry {RetryCount}", 
                        deliveryId, delivery.RetryCount);
                }
                else
                {
                    _logger.LogWarning("Failed to resend notification delivery {DeliveryId} on retry {RetryCount}/{MaxRetries}. Error: {Error}", 
                        deliveryId, delivery.RetryCount, maxRetries, result.ErrorMessage);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending failed notification {DeliveryId}", deliveryId);
                throw;
            }
        }

        private (int limit, TimeSpan window) GetRateLimitsForChannel(string channel)
        {
            return channel.ToLower() switch
            {
                NotificationChannelConstants.Email => (10, TimeSpan.FromHours(1)),
                NotificationChannelConstants.SMS => (5, TimeSpan.FromHours(1)),
                NotificationChannelConstants.Push => (20, TimeSpan.FromHours(1)),
                NotificationChannelConstants.WebPush => (20, TimeSpan.FromHours(1)),
                NotificationChannelConstants.Telegram => (10, TimeSpan.FromHours(1)),
                NotificationChannelConstants.SocialMedia => (5, TimeSpan.FromDays(1)),
                _ => (10, TimeSpan.FromHours(1))
            };
        }
    }
}

