using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Models;
using AmesaBackend.Notification.Services;
using AmesaBackend.Notification.Services.Interfaces;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Caching;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Notification.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationDbContext _context;
        private readonly INotificationOrchestrator _orchestrator;
        private readonly INotificationReadStateService _readStateService;
        private readonly INotificationPreferenceService _preferenceService;
        private readonly ILogger<NotificationController> _logger;
        private readonly ICache _cache;

        public NotificationController(
            NotificationDbContext context,
            INotificationOrchestrator orchestrator,
            INotificationReadStateService readStateService,
            INotificationPreferenceService preferenceService,
            ILogger<NotificationController> logger,
            ICache cache)
        {
            _context = context;
            _orchestrator = orchestrator;
            _readStateService = readStateService;
            _preferenceService = preferenceService;
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
                    .Where(n => n.UserId == userId && !n.IsDeleted)
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
                    .Where(n => n.Id == id && n.UserId == userId && !n.IsDeleted)
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

                // Verify notification exists and belongs to user (not soft-deleted)
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                // Get device info from headers
                var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
                var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

                // Use read state service for proper tracking and SignalR broadcast
                var success = await _readStateService.MarkAsReadAsync(id, userId, deviceId, userAgent, "web");

                if (!success)
                {
                    return StatusCode(500, new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to mark notification as read"
                    });
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
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead([FromQuery] string? notificationTypeCode = null)
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

                // Use read state service for proper tracking and SignalR broadcast
                var count = await _readStateService.MarkAllAsReadAsync(userId, notificationTypeCode);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"{count} notification(s) marked as read"
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

        [HttpPut("{id}/unread")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsUnread(Guid id)
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

                // Verify notification exists and belongs to user (not soft-deleted)
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                var success = await _readStateService.MarkAsUnreadAsync(id, userId);

                if (!success)
                {
                    return StatusCode(500, new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to mark notification as unread"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification marked as unread"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as unread", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to mark notification as unread"
                });
            }
        }

        [HttpGet("{id}/read-history")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<NotificationReadHistoryDto>>>> GetReadHistory(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<NotificationReadHistoryDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Verify notification exists and belongs to user (not soft-deleted)
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<List<NotificationReadHistoryDto>>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                var history = await _readStateService.GetReadHistoryAsync(id);

                var historyDto = history.Select(h => new NotificationReadHistoryDto
                {
                    Id = h.Id,
                    NotificationId = h.NotificationId,
                    UserId = h.UserId,
                    ReadAt = h.ReadAt,
                    DeviceId = h.DeviceId,
                    DeviceName = h.DeviceName,
                    Channel = h.Channel,
                    ReadMethod = h.ReadMethod
                }).ToList();

                return Ok(new ApiResponse<List<NotificationReadHistoryDto>>
                {
                    Success = true,
                    Data = historyDto,
                    Message = "Read history retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching read history for notification {NotificationId}", id);
                return StatusCode(500, new ApiResponse<List<NotificationReadHistoryDto>>
                {
                    Success = false,
                    Message = "Failed to fetch read history"
                });
            }
        }

        [HttpPost("sync-read-state")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> SyncReadState([FromBody] SyncReadStateRequest request)
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

                if (request == null || request.ReadNotificationIds == null || request.ReadNotificationIds.Count == 0)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Read notification IDs are required"
                    });
                }

                await _readStateService.SyncReadStateAsync(userId, request.ReadNotificationIds);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"Synced {request.ReadNotificationIds.Count} notification read state(s)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing read state");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to sync read state"
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
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                // Soft delete instead of hard delete
                notification.IsDeleted = true;
                notification.DeletedAt = DateTime.UtcNow;
                notification.DeletedBy = userIdClaim;
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

        [HttpGet("{id}/delivery-status-history")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<DeliveryStatusHistoryDto>>>> GetDeliveryStatusHistory(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<DeliveryStatusHistoryDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Verify notification belongs to user (not soft-deleted)
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<List<DeliveryStatusHistoryDto>>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                // Get all delivery status history for this notification
                var history = await _context.NotificationDeliveryStatusHistories
                    .Where(h => h.Delivery != null && h.Delivery.NotificationId == id)
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new DeliveryStatusHistoryDto
                    {
                        Id = h.Id,
                        DeliveryId = h.DeliveryId,
                        Status = h.Status,
                        ChangedAt = h.ChangedAt,
                        ChangedBy = h.ChangedBy,
                        Reason = h.Reason,
                        CreatedAt = h.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<DeliveryStatusHistoryDto>>
                {
                    Success = true,
                    Data = history,
                    Message = "Delivery status history retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching delivery status history for notification {NotificationId}", id);
                return StatusCode(500, new ApiResponse<List<DeliveryStatusHistoryDto>>
                {
                    Success = false,
                    Message = "Failed to fetch delivery status history"
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

                // Verify notification belongs to user (not soft-deleted)
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

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

                // Verify notification belongs to user (not soft-deleted)
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

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

        [HttpGet("preferences/features")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<FeaturePreferenceDto>>>> GetFeaturePreferences()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<FeaturePreferenceDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var preferences = await _preferenceService.GetFeaturePreferencesAsync(userId);

                var dtos = preferences.Select(p => new FeaturePreferenceDto
                {
                    Id = p.Id,
                    Feature = p.Feature,
                    Enabled = p.Enabled,
                    Channels = p.Channels.ToList(),
                    FrequencyLimit = p.FrequencyLimit,
                    FrequencyWindow = p.FrequencyWindow,
                    QuietHoursStart = p.QuietHoursStart,
                    QuietHoursEnd = p.QuietHoursEnd
                }).ToList();

                return Ok(new ApiResponse<List<FeaturePreferenceDto>>
                {
                    Success = true,
                    Data = dtos,
                    Message = "Feature preferences retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching feature preferences");
                return StatusCode(500, new ApiResponse<List<FeaturePreferenceDto>>
                {
                    Success = false,
                    Message = "Failed to fetch feature preferences"
                });
            }
        }

        [HttpPut("preferences/features")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<FeaturePreferenceDto>>> UpdateFeaturePreference([FromBody] UpdateFeaturePreferenceRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<FeaturePreferenceDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var preference = await _preferenceService.GetFeaturePreferenceAsync(userId, request.Feature);
                var enabled = request.Enabled ?? preference?.Enabled ?? true;
                var channels = request.Channels?.ToArray() ?? preference?.Channels;

                await _preferenceService.UpdateFeaturePreferenceAsync(
                    userId,
                    request.Feature,
                    enabled,
                    channels,
                    request.QuietHoursStart,
                    request.QuietHoursEnd,
                    request.FrequencyLimit,
                    request.FrequencyWindow);

                var updated = await _preferenceService.GetFeaturePreferenceAsync(userId, request.Feature);
                if (updated == null)
                {
                    return StatusCode(500, new ApiResponse<FeaturePreferenceDto>
                    {
                        Success = false,
                        Message = "Failed to retrieve updated preference"
                    });
                }

                var dto = new FeaturePreferenceDto
                {
                    Id = updated.Id,
                    Feature = updated.Feature,
                    Enabled = updated.Enabled,
                    Channels = updated.Channels.ToList(),
                    FrequencyLimit = updated.FrequencyLimit,
                    FrequencyWindow = updated.FrequencyWindow,
                    QuietHoursStart = updated.QuietHoursStart,
                    QuietHoursEnd = updated.QuietHoursEnd
                };

                return Ok(new ApiResponse<FeaturePreferenceDto>
                {
                    Success = true,
                    Data = dto,
                    Message = "Feature preference updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feature preference");
                return StatusCode(500, new ApiResponse<FeaturePreferenceDto>
                {
                    Success = false,
                    Message = "Failed to update feature preference"
                });
            }
        }

        [HttpGet("preferences/types")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<TypePreferenceDto>>>> GetTypePreferences()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<TypePreferenceDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var preferences = await _preferenceService.GetTypePreferencesAsync(userId);

                var dtos = preferences.Select(p => new TypePreferenceDto
                {
                    Id = p.Id,
                    NotificationTypeCode = p.NotificationTypeCode,
                    Enabled = p.Enabled,
                    Channels = p.Channels.ToList(),
                    Priority = p.Priority
                }).ToList();

                return Ok(new ApiResponse<List<TypePreferenceDto>>
                {
                    Success = true,
                    Data = dtos,
                    Message = "Type preferences retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching type preferences");
                return StatusCode(500, new ApiResponse<List<TypePreferenceDto>>
                {
                    Success = false,
                    Message = "Failed to fetch type preferences"
                });
            }
        }

        [HttpPut("preferences/types")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<TypePreferenceDto>>> UpdateTypePreference([FromBody] UpdateTypePreferenceRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<TypePreferenceDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var preference = await _preferenceService.GetTypePreferenceAsync(userId, request.NotificationTypeCode);
                var enabled = request.Enabled ?? preference?.Enabled ?? true;
                var channels = request.Channels?.ToArray() ?? preference?.Channels;
                var priority = request.Priority ?? preference?.Priority ?? 5;

                await _preferenceService.UpdateTypePreferenceAsync(
                    userId,
                    request.NotificationTypeCode,
                    enabled,
                    channels,
                    priority);

                var updated = await _preferenceService.GetTypePreferenceAsync(userId, request.NotificationTypeCode);
                if (updated == null)
                {
                    return StatusCode(500, new ApiResponse<TypePreferenceDto>
                    {
                        Success = false,
                        Message = "Failed to retrieve updated preference"
                    });
                }

                var dto = new TypePreferenceDto
                {
                    Id = updated.Id,
                    NotificationTypeCode = updated.NotificationTypeCode,
                    Enabled = updated.Enabled,
                    Channels = updated.Channels.ToList(),
                    Priority = updated.Priority
                };

                return Ok(new ApiResponse<TypePreferenceDto>
                {
                    Success = true,
                    Data = dto,
                    Message = "Type preference updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating type preference");
                return StatusCode(500, new ApiResponse<TypePreferenceDto>
                {
                    Success = false,
                    Message = "Failed to update type preference"
                });
            }
        }
    }

    public class ResendNotificationRequest
    {
        public string? Channel { get; set; }
    }

    // Feature and Type Preference DTOs
    public class FeaturePreferenceDto
    {
        public Guid Id { get; set; }
        public string Feature { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public List<string> Channels { get; set; } = new();
        public int? FrequencyLimit { get; set; }
        public string? FrequencyWindow { get; set; }
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
    }

    public class TypePreferenceDto
    {
        public Guid Id { get; set; }
        public string NotificationTypeCode { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public List<string> Channels { get; set; } = new();
        public int Priority { get; set; }
    }

    public class UpdateFeaturePreferenceRequest
    {
        [Required]
        [StringLength(100)]
        public string Feature { get; set; } = string.Empty;
        public bool? Enabled { get; set; }
        public List<string>? Channels { get; set; }
        public int? FrequencyLimit { get; set; }
        public string? FrequencyWindow { get; set; }
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
    }

    public class UpdateTypePreferenceRequest
    {
        [Required]
        [StringLength(100)]
        public string NotificationTypeCode { get; set; } = string.Empty;
        public bool? Enabled { get; set; }
        public List<string>? Channels { get; set; }
        public int? Priority { get; set; }
    }

    [ApiController]
    [Route("api/v1/devices")]
    [Authorize]
    public class DeviceRegistrationController : ControllerBase
    {
        private readonly IDeviceRegistrationService _deviceRegistrationService;
        private readonly ILogger<DeviceRegistrationController> _logger;

        public DeviceRegistrationController(
            IDeviceRegistrationService deviceRegistrationService,
            ILogger<DeviceRegistrationController> logger)
        {
            _deviceRegistrationService = deviceRegistrationService;
            _logger = logger;
        }

        /// <summary>
        /// Register a device for push notifications
        /// POST /api/v1/devices/register
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<DeviceRegistrationDto>>> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<DeviceRegistrationDto>
                    {
                        Success = false,
                        Message = "Authentication required"
                    });
                }

                var registration = await _deviceRegistrationService.RegisterDeviceAsync(
                    userId,
                    request.DeviceToken,
                    request.Platform,
                    request.DeviceId,
                    request.DeviceName,
                    request.AppVersion);

                var dto = new DeviceRegistrationDto
                {
                    Id = registration.Id,
                    UserId = registration.UserId,
                    DeviceToken = registration.DeviceToken,
                    Platform = registration.Platform,
                    DeviceId = registration.DeviceId,
                    DeviceName = registration.DeviceName,
                    AppVersion = registration.AppVersion,
                    IsActive = registration.IsActive,
                    CreatedAt = registration.CreatedAt,
                    UpdatedAt = registration.UpdatedAt,
                    LastUsedAt = registration.LastUsedAt
                };

                return Ok(new ApiResponse<DeviceRegistrationDto>
                {
                    Success = true,
                    Data = dto,
                    Message = "Device registered successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device");
                return StatusCode(500, new ApiResponse<DeviceRegistrationDto>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred registering device" }
                });
            }
        }

        /// <summary>
        /// Unregister a device
        /// DELETE /api/v1/devices/unregister
        /// </summary>
        [HttpDelete("unregister")]
        public async Task<ActionResult<ApiResponse<object>>> UnregisterDevice([FromBody] UnregisterDeviceRequest request)
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Authentication required"
                    });
                }

                var success = await _deviceRegistrationService.UnregisterDeviceAsync(userId, request.DeviceToken);

                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Device not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Device unregistered successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering device");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred unregistering device" }
                });
            }
        }

        /// <summary>
        /// Get user's registered devices
        /// GET /api/v1/devices
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DeviceRegistrationDto>>>> GetUserDevices()
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<DeviceRegistrationDto>>
                    {
                        Success = false,
                        Message = "Authentication required"
                    });
                }

                var devices = await _deviceRegistrationService.GetUserDevicesAsync(userId);
                var dtos = devices.Select(d => new DeviceRegistrationDto
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    DeviceToken = d.DeviceToken,
                    Platform = d.Platform,
                    DeviceId = d.DeviceId,
                    DeviceName = d.DeviceName,
                    AppVersion = d.AppVersion,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    LastUsedAt = d.LastUsedAt
                }).ToList();

                return Ok(new ApiResponse<List<DeviceRegistrationDto>>
                {
                    Success = true,
                    Data = dtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user devices");
                return StatusCode(500, new ApiResponse<List<DeviceRegistrationDto>>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving devices" }
                });
            }
        }
    }

    // DTOs for device registration
    public class RegisterDeviceRequest
    {
        [Required]
        public string DeviceToken { get; set; } = string.Empty;

        [Required]
        public string Platform { get; set; } = string.Empty; // "iOS", "Android", "Web"

        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? AppVersion { get; set; }
    }

    public class UnregisterDeviceRequest
    {
        [Required]
        public string DeviceToken { get; set; } = string.Empty;
    }

    public class DeviceRegistrationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? AppVersion { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}

