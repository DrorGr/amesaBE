using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AmesaBackend.Analytics.Services;
using AmesaBackend.Shared.Helpers;

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
    }
}

