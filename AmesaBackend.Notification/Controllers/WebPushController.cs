using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Models;
using System.Security.Claims;

namespace AmesaBackend.Notification.Controllers
{
    [ApiController]
    [Route("api/v1/notifications/web-push")]
    public class WebPushController : ControllerBase
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<WebPushController> _logger;

        public WebPushController(
            NotificationDbContext context,
            ILogger<WebPushController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("subscribe")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PushSubscriptionDto>>> Subscribe([FromBody] SubscribePushRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<PushSubscriptionDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Check if subscription already exists
                var existing = await _context.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Endpoint);

                if (existing != null)
                {
                    // Update existing subscription
                    existing.P256dhKey = request.P256dhKey;
                    existing.AuthKey = request.AuthKey;
                    existing.UserAgent = request.UserAgent;
                    existing.DeviceInfo = request.DeviceInfo != null 
                        ? System.Text.Json.JsonSerializer.Serialize(request.DeviceInfo) 
                        : null;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new subscription
                    var subscription = new PushSubscription
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Endpoint = request.Endpoint,
                        P256dhKey = request.P256dhKey,
                        AuthKey = request.AuthKey,
                        UserAgent = request.UserAgent,
                        DeviceInfo = request.DeviceInfo != null 
                            ? System.Text.Json.JsonSerializer.Serialize(request.DeviceInfo) 
                            : null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.PushSubscriptions.Add(subscription);
                }

                await _context.SaveChangesAsync();

                var subscriptionDto = new PushSubscriptionDto
                {
                    Id = existing?.Id ?? Guid.NewGuid(),
                    UserId = userId,
                    Endpoint = request.Endpoint,
                    DeviceInfo = request.DeviceInfo,
                    CreatedAt = DateTime.UtcNow
                };

                return Ok(new ApiResponse<PushSubscriptionDto>
                {
                    Success = true,
                    Data = subscriptionDto,
                    Message = "Push subscription created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to web push");
                return StatusCode(500, new ApiResponse<PushSubscriptionDto>
                {
                    Success = false,
                    Message = "Failed to subscribe to web push"
                });
            }
        }

        [HttpPost("unsubscribe")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> Unsubscribe([FromBody] UnsubscribePushRequest request)
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

                var subscription = await _context.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Endpoint);

                if (subscription == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Subscription not found"
                    });
                }

                _context.PushSubscriptions.Remove(subscription);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Push subscription removed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from web push");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to unsubscribe from web push"
                });
            }
        }

        [HttpGet("subscriptions")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<PushSubscriptionDto>>>> GetSubscriptions()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<PushSubscriptionDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var subscriptions = await _context.PushSubscriptions
                    .Where(s => s.UserId == userId)
                    .Select(s => new PushSubscriptionDto
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        Endpoint = s.Endpoint,
                        DeviceInfo = s.DeviceInfo != null 
                            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(s.DeviceInfo, new System.Text.Json.JsonSerializerOptions()) 
                            : null,
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<PushSubscriptionDto>>
                {
                    Success = true,
                    Data = subscriptions,
                    Message = "Subscriptions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching push subscriptions");
                return StatusCode(500, new ApiResponse<List<PushSubscriptionDto>>
                {
                    Success = false,
                    Message = "Failed to fetch subscriptions"
                });
            }
        }
    }
}

