using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Lottery.Services;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers;

[ApiController]
[Route("api/v1/gamification")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;
    private readonly ILogger<GamificationController> _logger;

    public GamificationController(
        IGamificationService gamificationService,
        ILogger<GamificationController> logger)
    {
        _gamificationService = gamificationService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value 
            ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        return userId;
    }

    /// <summary>
    /// Get gamification data for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetGamificationData()
    {
        // #region agent log
        _logger.LogInformation("[DEBUG] GetGamificationData entry - serviceNull={ServiceNull}", _gamificationService == null);
        // #endregion
        try
        {
            var userId = GetUserId();
            // #region agent log
            _logger.LogInformation("[DEBUG] Before GetUserGamificationDataAsync - userId={UserId}", userId);
            // #endregion
            // Call the gamification service to get user's gamification data
            // Note: Adjust this based on the actual IGamificationService interface
            var data = await _gamificationService.GetUserGamificationDataAsync(userId);
            // #region agent log
            _logger.LogInformation("[DEBUG] After GetUserGamificationDataAsync - dataNull={DataNull}", data == null);
            // #endregion

            return Ok(new
            {
                success = true,
                data = data,
                message = "Gamification data retrieved successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { success = false, error = new { message = ex.Message } });
        }
        catch (Exception ex)
        {
            // #region agent log
            _logger.LogError(ex, "[DEBUG] Exception in GetGamificationData - Type={ExceptionType}, Message={Message}", ex.GetType().Name, ex.Message);
            // #endregion
            _logger.LogError(ex, "Error fetching gamification data");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching gamification data" } });
        }
    }
}

