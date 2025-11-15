using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AmesaBackend.Analytics.Services;

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
        public async Task<ActionResult<object>> GetDashboardAnalytics()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var analytics = await _analyticsService.GetDashboardAnalyticsAsync(userId);
                return Ok(new { Success = true, Data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard analytics");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("lottery-stats")]
        public async Task<ActionResult<object>> GetLotteryStats([FromQuery] Guid? houseId)
        {
            try
            {
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

