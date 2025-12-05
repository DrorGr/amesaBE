using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Models;
using AmesaBackend.Notification.Services;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Caching;
using System.Security.Claims;

namespace AmesaBackend.Notification.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationDbContext _context;
        private readonly INotificationOrchestrator _orchestrator;
        private readonly ILogger<NotificationController> _logger;
        private readonly ICache _cache;

        public NotificationController(
            NotificationDbContext context,
            INotificationOrchestrator orchestrator,
            ILogger<NotificationController> logger,
            ICache cache)
        {
            _context = context;
            _orchestrator = orchestrator;
            _logger = logger;
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] bool? unreadOnly = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<NotificationDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var query = _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .AsQueryable();

                if (unreadOnly == true)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var total = await query.CountAsync();
                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        UserId = n.UserId,
                        TemplateId = n.TemplateId,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        IsRead = n.IsRead,
                        ReadAt = n.ReadAt,
                        Data = n.Data != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(n.Data, new System.Text.Json.JsonSerializerOptions()) : null,
                        CreatedAt = n.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<NotificationDto>>
                {
                    Success = true,
                    Data = notifications,
                    Message = "Notifications retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return StatusCode(500, new ApiResponse<List<NotificationDto>>
                {
                    Success = false,
                    Message = "Failed to fetch notifications"
                });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> GetNotification(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<NotificationDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var notification = await _context.UserNotifications
                    .Where(n => n.Id == id && n.UserId == userId)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        UserId = n.UserId,
                        TemplateId = n.TemplateId,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        IsRead = n.IsRead,
                        ReadAt = n.ReadAt,
                        Data = n.Data != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(n.Data, new System.Text.Json.JsonSerializerOptions()) : null,
                        CreatedAt = n.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (notification == null)
                {
                    return NotFound(new ApiResponse<NotificationDto>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                return Ok(new ApiResponse<NotificationDto>
                {
                    Success = true,
                    Data = notification,
                    Message = "Notification retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notification {NotificationId}", id);
                return StatusCode(500, new ApiResponse<NotificationDto>
                {
                    Success = false,
                    Message = "Failed to fetch notification"
                });
            }
        }

        [HttpPut("{id}/read")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to mark notification as read"
                });
            }
        }

        [HttpPut("read-all")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var notifications = await _context.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = now;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"{notifications.Count} notification(s) marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to mark all notifications as read"
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                _context.UserNotifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete notification"
                });
            }
        }

        [HttpPost("send-multi-channel")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<OrchestrationResult>>> SendMultiChannelNotification([FromBody] SendNotificationRequest request)
        {
            try
            {
                if (request.Channels == null || request.Channels.Count == 0)
                {
                    return BadRequest(new ApiResponse<OrchestrationResult>
                    {
                        Success = false,
                        Message = "At least one channel must be specified"
                    });
                }

                var notificationRequest = new NotificationRequest
                {
                    UserId = request.UserId,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    Data = request.Data,
                    TemplateName = request.TemplateName,
                    TemplateVariables = request.TemplateVariables
                };

                var result = await _orchestrator.SendMultiChannelAsync(request.UserId, notificationRequest, request.Channels);

                return Ok(new ApiResponse<OrchestrationResult>
                {
                    Success = true,
                    Data = result,
                    Message = $"Notification sent to {result.SuccessCount} channel(s)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending multi-channel notification");
                return StatusCode(500, new ApiResponse<OrchestrationResult>
                {
                    Success = false,
                    Message = "Failed to send notification"
                });
            }
        }

        [HttpGet("{id}/delivery-status")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<DeliveryStatusDto>>>> GetDeliveryStatus(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<DeliveryStatusDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Verify notification belongs to user
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<List<DeliveryStatusDto>>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                var statuses = await _orchestrator.GetDeliveryStatusAsync(id);

                return Ok(new ApiResponse<List<DeliveryStatusDto>>
                {
                    Success = true,
                    Data = statuses,
                    Message = "Delivery status retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching delivery status for notification {NotificationId}", id);
                return StatusCode(500, new ApiResponse<List<DeliveryStatusDto>>
                {
                    Success = false,
                    Message = "Failed to fetch delivery status"
                });
            }
        }

        [HttpPost("{id}/resend")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> ResendNotification(Guid id, [FromBody] ResendNotificationRequest? request = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Verify notification belongs to user
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                // Get failed deliveries
                var failedDeliveries = await _context.NotificationDeliveries
                    .Where(d => d.NotificationId == id && d.Status == "failed")
                    .ToListAsync();

                if (failedDeliveries.Count == 0)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "No failed deliveries to resend"
                    });
                }

                foreach (var delivery in failedDeliveries)
                {
                    await _orchestrator.ResendFailedNotificationAsync(delivery.Id);
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification resend initiated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending notification {NotificationId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to resend notification"
                });
            }
        }

        [HttpGet("preferences/channels")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<ChannelPreferencesDto>>>> GetChannelPreferences()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<ChannelPreferencesDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var preferences = await _context.UserChannelPreferences
                    .Where(p => p.UserId == userId)
                    .Select(p => new ChannelPreferencesDto
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        Channel = p.Channel,
                        Enabled = p.Enabled,
                        NotificationTypes = p.NotificationTypes != null 
                            ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.NotificationTypes, new System.Text.Json.JsonSerializerOptions()) 
                            : null,
                        QuietHoursStart = p.QuietHoursStart,
                        QuietHoursEnd = p.QuietHoursEnd
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<ChannelPreferencesDto>>
                {
                    Success = true,
                    Data = preferences,
                    Message = "Channel preferences retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching channel preferences");
                return StatusCode(500, new ApiResponse<List<ChannelPreferencesDto>>
                {
                    Success = false,
                    Message = "Failed to fetch channel preferences"
                });
            }
        }

        [HttpPut("preferences/channels")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ChannelPreferencesDto>>> UpdateChannelPreferences([FromBody] UpdateChannelPreferencesRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<ChannelPreferencesDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var preference = await _context.UserChannelPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Channel == request.Channel);

                if (preference == null)
                {
                    preference = new UserChannelPreference
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Channel = request.Channel,
                        Enabled = request.Enabled ?? true,
                        NotificationTypes = request.NotificationTypes != null 
                            ? System.Text.Json.JsonSerializer.Serialize(request.NotificationTypes) 
                            : null,
                        QuietHoursStart = request.QuietHoursStart,
                        QuietHoursEnd = request.QuietHoursEnd,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserChannelPreferences.Add(preference);
                }
                else
                {
                    if (request.Enabled.HasValue)
                        preference.Enabled = request.Enabled.Value;
                    if (request.NotificationTypes != null)
                        preference.NotificationTypes = System.Text.Json.JsonSerializer.Serialize(request.NotificationTypes);
                    if (request.QuietHoursStart.HasValue)
                        preference.QuietHoursStart = request.QuietHoursStart;
                    if (request.QuietHoursEnd.HasValue)
                        preference.QuietHoursEnd = request.QuietHoursEnd;
                    preference.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var dto = new ChannelPreferencesDto
                {
                    Id = preference.Id,
                    UserId = preference.UserId,
                    Channel = preference.Channel,
                    Enabled = preference.Enabled,
                    NotificationTypes = preference.NotificationTypes != null 
                        ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(preference.NotificationTypes) 
                        : null,
                    QuietHoursStart = preference.QuietHoursStart,
                    QuietHoursEnd = preference.QuietHoursEnd
                };

                return Ok(new ApiResponse<ChannelPreferencesDto>
                {
                    Success = true,
                    Data = dto,
                    Message = "Channel preferences updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating channel preferences");
                return StatusCode(500, new ApiResponse<ChannelPreferencesDto>
                {
                    Success = false,
                    Message = "Failed to update channel preferences"
                });
            }
        }

        /// <summary>
        /// Internal endpoint for syncing notification preferences from Auth service
        /// Accepts userId in request body for service-to-service communication
        /// </summary>
        [HttpPut("preferences/channels/sync")]
        public async Task<ActionResult<ApiResponse<object>>> SyncChannelPreferences([FromBody] SyncChannelPreferencesRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.UserId == Guid.Empty)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "UserId is required"
                    });
                }

                // Process each channel preference
                foreach (var channelPref in request.ChannelPreferences)
                {
                    var preference = await _context.UserChannelPreferences
                        .FirstOrDefaultAsync(p => p.UserId == request.UserId && p.Channel == channelPref.Channel);

                    if (preference == null)
                    {
                        preference = new UserChannelPreference
                        {
                            Id = Guid.NewGuid(),
                            UserId = request.UserId,
                            Channel = channelPref.Channel,
                            Enabled = channelPref.Enabled ?? true,
                            NotificationTypes = channelPref.NotificationTypes != null 
                                ? System.Text.Json.JsonSerializer.Serialize(channelPref.NotificationTypes) 
                                : null,
                            QuietHoursStart = channelPref.QuietHoursStart,
                            QuietHoursEnd = channelPref.QuietHoursEnd,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.UserChannelPreferences.Add(preference);
                    }
                    else
                    {
                        if (channelPref.Enabled.HasValue)
                            preference.Enabled = channelPref.Enabled.Value;
                        if (channelPref.NotificationTypes != null)
                            preference.NotificationTypes = System.Text.Json.JsonSerializer.Serialize(channelPref.NotificationTypes);
                        if (channelPref.QuietHoursStart.HasValue)
                            preference.QuietHoursStart = channelPref.QuietHoursStart;
                        if (channelPref.QuietHoursEnd.HasValue)
                            preference.QuietHoursEnd = channelPref.QuietHoursEnd;
                        preference.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Synced notification preferences for user {UserId} from Auth service", request.UserId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Channel preferences synced successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing channel preferences");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to sync channel preferences"
                });
            }
        }
    }

    public class ResendNotificationRequest
    {
        public string? Channel { get; set; }
    }
}

