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
            IHubContext<NotificationHub>? hubContext = null)
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
            _hubContext = hubContext;
        }

        public async Task<OrchestrationResult> SendMultiChannelAsync(Guid userId, NotificationRequest request, List<string> channels)
        {
            var result = new OrchestrationResult
            {
                NotificationId = Guid.NewGuid()
            };

            try
            {
                // Set default language if not provided
                if (string.IsNullOrEmpty(request.Language))
                {
                    request.Language = NotificationChannelConstants.DefaultLanguage;
                }
                // Get user channel preferences from cache or database
                var cacheKey = $"{NotificationChannelConstants.CacheKeyUserPrefs}{userId}";
                var cachedPrefs = await _cache.GetRecordAsync<Dictionary<string, object>>(cacheKey);
                
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
                    
                    // Cache for 15 minutes
                    if (cachedPrefs.Count > 0)
                    {
                        await _cache.SetRecordAsync(cacheKey, cachedPrefs, TimeSpan.FromMinutes(15));
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
                    Title = request.Title,
                    Message = request.Message,
                    Data = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserNotifications.Add(notification);

                // Process each channel
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

                    // Check if channel is enabled for user
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

                    // Increment rate limit
                    await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, rateLimits.window);

                    // Send notification via channel
                    var deliveryResult = await provider.SendAsync(request);

                    // Create delivery record
                    var delivery = new NotificationDelivery
                    {
                        Id = Guid.NewGuid(),
                        NotificationId = result.NotificationId,
                        Channel = channelName,
                        Status = deliveryResult.Success ? NotificationChannelConstants.StatusSent : NotificationChannelConstants.StatusFailed,
                        ExternalId = deliveryResult.ExternalId,
                        ErrorMessage = deliveryResult.ErrorMessage,
                        Cost = deliveryResult.Cost,
                        Currency = NotificationChannelConstants.DefaultCurrency,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (deliveryResult.Success)
                    {
                        delivery.DeliveredAt = DateTime.UtcNow;
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailureCount++;
                    }

                    _context.NotificationDeliveries.Add(delivery);
                    result.DeliveryResults.Add(deliveryResult);

                    // Publish EventBridge event
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
                        _logger.LogWarning(ex, "Failed to publish EventBridge event for notification {NotificationId}", result.NotificationId);
                    }
                }

                await _context.SaveChangesAsync();

                // Send real-time notification via SignalR
                if (_hubContext != null)
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send SignalR notification for user {UserId}", userId);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMultiChannelAsync for user {UserId}", userId);
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

                var provider = _channelProviders.FirstOrDefault(p => p.ChannelName.Equals(delivery.Channel, StringComparison.OrdinalIgnoreCase));
                if (provider == null)
                {
                    _logger.LogWarning("Channel provider not found: {Channel}", delivery.Channel);
                    return;
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

