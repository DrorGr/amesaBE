using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class WatchlistController : ControllerBase
    {
        private readonly IWatchlistService _watchlistService;
        private readonly ILogger<WatchlistController> _logger;

        public WatchlistController(
            IWatchlistService watchlistService,
            ILogger<WatchlistController> logger)
        {
            _watchlistService = watchlistService;
            _logger = logger;
        }

        /// <summary>
        /// Get user's watchlist
        /// GET /api/v1/watchlist
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<WatchlistItemDto>>>> GetWatchlist()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<WatchlistItemDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var watchlistItems = await _watchlistService.GetUserWatchlistItemsAsync(userId);

                return Ok(new ApiResponse<List<WatchlistItemDto>>
                {
                    Success = true,
                    Data = watchlistItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving watchlist");
                return StatusCode(500, new ApiResponse<List<WatchlistItemDto>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Add house to watchlist
        /// POST /api/v1/watchlist/{id}
        /// </summary>
        [HttpPost("{id}")]
        public async Task<ActionResult<ApiResponse<WatchlistItemDto>>> AddToWatchlist(
            Guid id, 
            [FromBody] AddToWatchlistRequest? request = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<WatchlistItemDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var notificationEnabled = request?.NotificationEnabled ?? true;
                await _watchlistService.AddToWatchlistAsync(userId, id, notificationEnabled);

                // Fetch the newly created watchlist item
                var watchlistItems = await _watchlistService.GetUserWatchlistItemsAsync(userId);
                var watchlistItem = watchlistItems.FirstOrDefault(w => w.HouseId == id);
                
                if (watchlistItem == null)
                {
                    // Fallback if not found (shouldn't happen)
                    watchlistItem = new WatchlistItemDto
                    {
                        Id = Guid.NewGuid(),
                        HouseId = id,
                        House = new HouseDto
                        {
                            Id = id,
                            UniqueParticipants = 0,
                            IsParticipantCapReached = false
                        },
                        NotificationEnabled = notificationEnabled,
                        AddedAt = DateTime.UtcNow
                    };
                }

                return Ok(new ApiResponse<WatchlistItemDto>
                {
                    Success = true,
                    Data = watchlistItem,
                    Message = "House added to watchlist"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<WatchlistItemDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<WatchlistItemDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Error = new ErrorResponse
                    {
                        Code = "WATCHLIST_ITEM_EXISTS",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding house {HouseId} to watchlist", id);
                return StatusCode(500, new ApiResponse<WatchlistItemDto>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Remove house from watchlist
        /// DELETE /api/v1/watchlist/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveFromWatchlist(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                await _watchlistService.RemoveFromWatchlistAsync(userId, id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "House removed from watchlist"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    Error = new ErrorResponse
                    {
                        Code = "WATCHLIST_ITEM_NOT_FOUND",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing house {HouseId} from watchlist", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Toggle notification for watchlist item
        /// PUT /api/v1/watchlist/{id}/notification
        /// </summary>
        [HttpPut("{id}/notification")]
        public async Task<ActionResult<ApiResponse<object>>> ToggleNotification(
            Guid id,
            [FromBody] ToggleNotificationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                await _watchlistService.ToggleNotificationAsync(userId, id, request.Enabled);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Notifications {(request.Enabled ? "enabled" : "disabled")} for watchlist item"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    Error = new ErrorResponse
                    {
                        Code = "WATCHLIST_ITEM_NOT_FOUND",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling notification for house {HouseId} in watchlist", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Get watchlist count
        /// GET /api/v1/watchlist/count
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<ApiResponse<int>>> GetWatchlistCount()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<int>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var count = await _watchlistService.GetWatchlistCountAsync(userId);

                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Data = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting watchlist count");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }
    }
}

