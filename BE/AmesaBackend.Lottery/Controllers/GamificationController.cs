using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Shared.Contracts;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers;

/// <summary>
/// Controller for managing gamification features in the lottery system.
/// Provides endpoints for retrieving user gamification data including points, levels, achievements, and streaks.
/// </summary>
[ApiController]
[Route("api/v1/gamification")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;
    private readonly ILogger<GamificationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GamificationController"/> class.
    /// </summary>
    /// <param name="gamificationService">Service for managing gamification features.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public GamificationController(
        IGamificationService gamificationService,
        ILogger<GamificationController> logger)
    {
        _gamificationService = gamificationService;
        _logger = logger;
    }

    /// <summary>
    /// Extracts the user ID from the current authentication token.
    /// </summary>
    /// <returns>The user's unique identifier as a Guid.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user ID cannot be found in the authentication token.</exception>
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
    /// Retrieves comprehensive gamification data for the authenticated user.
    /// Includes points, level, achievements, streaks, and other gamification metrics.
    /// </summary>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: User gamification DTO with all gamification metrics
    /// </returns>
    /// <response code="200">Successfully retrieved gamification data.</response>
    /// <response code="401">User is not authenticated or token is invalid.</response>
    /// <response code="503">Gamification service is not available.</response>
    /// <response code="500">An error occurred while processing the request.</response>
    [HttpGet]
    public async Task<ActionResult> GetGamificationData()
    {
        if (_gamificationService == null)
        {
            _logger.LogError("GamificationService is not available");
            return StatusCode(503, new { success = false, error = new { message = "Gamification service is not available" } });
        }
        
        try
        {
            var userId = GetUserId();
            // Call the gamification service to get user's gamification data
            var gamification = await _gamificationService.GetUserGamificationAsync(userId);

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
            _logger.LogError(ex, "Error fetching gamification data");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching gamification data", details = ex.Message } });
        }
    }
}

