using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AmesaBackend.Analytics.Services;
using AmesaBackend.Analytics.Services.Interfaces;
using AmesaBackend.Shared.Helpers;
using AmesaBackend.Analytics.DTOs;

namespace AmesaBackend.Analytics.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboardAnalytics([FromQuery] int? page = null, [FromQuery] int? limit = null)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new { Success = false, Message = "Authentication required" });
                }

                // Validate pagination parameters
                if (page.HasValue && page.Value < 1)
                {
                    return BadRequest(new { Success = false, Message = "Page number must be greater than 0" });
                }

                if (limit.HasValue && limit.Value < 1)
                {
                    return BadRequest(new { Success = false, Message = "Limit must be greater than 0" });
                }

                const int maxLimit = 100;
                if (limit.HasValue && limit.Value > maxLimit)
                {
                    return BadRequest(new { Success = false, Message = $"Limit cannot exceed {maxLimit}" });
                }

                var analytics = await _analyticsService.GetDashboardAnalyticsAsync(userId, page, limit);
                return Ok(new { Success = true, Data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard analytics");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("lottery-stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetLotteryStats([FromQuery] Guid? houseId)
        {
            try
            {
                // Verify user has admin role (additional check for security)
                if (!User.IsInRole("Admin"))
                {
                    _logger.LogWarning("Non-admin user attempted to access lottery stats: {UserId}", 
                        User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    return Forbid("Access denied. Admin role required.");
                }

                var stats = await _analyticsService.GetLotteryStatsAsync(houseId);
                return Ok(new { Success = true, Data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lottery stats");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<PagedResponse<UserSessionDto>>> GetSessions([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new PagedResponse<UserSessionDto>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                var sessions = await _analyticsService.GetUserSessionsAsync(userId, fromDate, toDate);
                return Ok(new PagedResponse<UserSessionDto>
                {
                    Success = true,
                    Items = sessions,
                    Total = sessions.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sessions");
                return StatusCode(500, new PagedResponse<UserSessionDto>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "Failed to retrieve sessions" }
                });
            }
        }

        [HttpGet("sessions/{id:guid}")]
        public async Task<ActionResult<ApiResponse<UserSessionDto>>> GetSession(Guid id)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<UserSessionDto>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                var session = await _analyticsService.GetUserSessionAsync(userId, id);
                if (session == null)
                {
                    return NotFound(new ApiResponse<UserSessionDto>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "NOT_FOUND", Message = "Session not found" }
                    });
                }

                return Ok(new ApiResponse<UserSessionDto>
                {
                    Success = true,
                    Data = session
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session {SessionId}", id);
                return StatusCode(500, new ApiResponse<UserSessionDto>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "Failed to retrieve session" }
                });
            }
        }

        [HttpGet("activity")]
        public async Task<ActionResult<PagedResponse<ActivityLogDto>>> GetActivity([FromQuery] ActivityFilters filters)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new PagedResponse<ActivityLogDto>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                var items = await _analyticsService.GetActivityAsync(userId, filters);
                return Ok(new PagedResponse<ActivityLogDto>
                {
                    Success = true,
                    Items = items,
                    Total = items.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity");
                return StatusCode(500, new PagedResponse<ActivityLogDto>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "Failed to retrieve activity" }
                });
            }
        }
    }
}

