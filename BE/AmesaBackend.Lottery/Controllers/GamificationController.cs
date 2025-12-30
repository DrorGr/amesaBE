using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Shared.Contracts;
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
        
        if (_gamificationService == null)
        {
            _logger.LogError("GamificationService is not available");
            return StatusCode(503, new { success = false, error = new { message = "Gamification service is not available" } });
        }
        
        try
        {
            var userId = GetUserId();
            // #region agent log
            _logger.LogInformation("[DEBUG] Before GetUserGamificationDataAsync - userId={UserId}", userId);
            // #endregion
            // Call the gamification service to get user's gamification data
            var gamification = await _gamificationService.GetUserGamificationAsync(userId);
            // #region agent log
            _logger.LogInformation("[DEBUG] After GetUserGamificationAsync - dataNull={DataNull}", gamification == null);
            // #endregion

            return Ok(new StandardApiResponse<UserGamificationDto>
            {
                Success = true,
                Data = gamification
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
            _logger.LogError(ex, "[DEBUG] Exception in GetGamificationData - Type={ExceptionType}, Message={Message}, StackTrace={StackTrace}", 
                ex.GetType().Name, ex.Message, ex.StackTrace);
            // #endregion
            _logger.LogError(ex, "Error fetching gamification data");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching gamification data", details = ex.Message } });
        }
    }
}

