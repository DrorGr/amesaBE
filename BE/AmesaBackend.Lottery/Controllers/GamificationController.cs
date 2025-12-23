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
        try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C,D", location = "GamificationController.cs:42", message = "GetGamificationData entry", data = new { serviceNull = _gamificationService == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        try
        {
            var userId = GetUserId();
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "GamificationController.cs:50", message = "Before GetUserGamificationDataAsync", data = new { userId = userId.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            // Call the gamification service to get user's gamification data
            // Note: Adjust this based on the actual IGamificationService interface
            var data = await _gamificationService.GetUserGamificationDataAsync(userId);
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "GamificationController.cs:52", message = "After GetUserGamificationDataAsync", data = new { dataNull = data == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
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
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C,D", location = "GamificationController.cs:67", message = "Exception caught", data = new { exceptionType = ex.GetType().Name, message = ex.Message, stackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length ?? 0)) }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            _logger.LogError(ex, "Error fetching gamification data");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching gamification data" } });
        }
    }
}

