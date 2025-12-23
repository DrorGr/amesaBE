using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers;

[ApiController]
[Route("api/v1/promotions")]
public class PromotionController : ControllerBase
{
    private readonly ILogger<PromotionController> _logger;

    public PromotionController(ILogger<PromotionController> logger)
    {
        _logger = logger;
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value 
            ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        
        return userId;
    }

    /// <summary>
    /// Get available promotions for the current user
    /// </summary>
    [HttpGet("available")]
    [Authorize]
    public async Task<ActionResult> GetAvailablePromotions([FromQuery] Guid? houseId = null)
    {
        // PromotionService is excluded from compilation (see .csproj)
        // Return service unavailable response
        return StatusCode(503, new 
        { 
            success = false, 
            error = new { message = "Promotion service is not available" } 
        });
    }
}

