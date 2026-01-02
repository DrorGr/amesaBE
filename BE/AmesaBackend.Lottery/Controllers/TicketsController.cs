using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers;

/// <summary>
/// Controller for managing lottery tickets.
/// Provides endpoints for retrieving user tickets and ticket analytics.
/// </summary>
[ApiController]
[Route("api/v1/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly LotteryDbContext _context;
    private readonly ILogger<TicketsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TicketsController"/> class.
    /// </summary>
    /// <param name="context">Database context for lottery data access.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public TicketsController(
        LotteryDbContext context,
        ILogger<TicketsController> logger)
    {
        _context = context;
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
    /// Retrieves all active lottery tickets for the authenticated user.
    /// Active tickets are those that are currently valid and have not been used or expired.
    /// </summary>
    /// <returns>
    /// A response containing:
    /// - success: Boolean indicating operation success
    /// - data: List of active lottery tickets with associated house information
    /// - message: Success message
    /// </returns>
    /// <response code="200">Successfully retrieved active tickets.</response>
    /// <response code="401">User is not authenticated or token is invalid.</response>
    /// <response code="503">Database service is not available.</response>
    /// <response code="500">An error occurred while processing the request.</response>
    [HttpGet("active")]
    public async Task<ActionResult> GetActiveTickets()
    {
        if (_context == null)
        {
            _logger.LogError("LotteryDbContext is not available");
            return StatusCode(503, new { success = false, error = new { message = "Database service is not available" } });
        }
        
        try
        {
            var userId = GetUserId();
            // Use Select() to only query properties that exist in the model
            // This avoids EF Core trying to SELECT promotion_code and discount_amount which don't exist in the model
            var activeTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId && t.Status == TicketStatus.Active)
                .Include(t => t.House)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = activeTickets,
                message = "Active tickets retrieved successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { success = false, error = new { message = ex.Message } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active tickets - InnerException: {InnerException}", ex.InnerException?.Message);
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching active tickets", details = ex.Message } });
        }
    }

    /// <summary>
    /// Retrieves analytics and statistics for the authenticated user's lottery tickets.
    /// Provides aggregated data including total tickets, active tickets, and participation metrics.
    /// </summary>
    /// <returns>
    /// A response containing:
    /// - success: Boolean indicating operation success
    /// - data: Object containing ticket analytics (total tickets, active tickets, etc.)
    /// - message: Success message
    /// </returns>
    /// <response code="200">Successfully retrieved ticket analytics.</response>
    /// <response code="401">User is not authenticated or token is invalid.</response>
    /// <response code="503">Database service is not available.</response>
    /// <response code="500">An error occurred while processing the request.</response>
    [HttpGet("analytics")]
    public async Task<ActionResult> GetTicketAnalytics()
    {
        if (_context == null)
        {
            _logger.LogError("LotteryDbContext is not available");
            return StatusCode(503, new { success = false, error = new { message = "Database service is not available" } });
        }
        
        try
        {
            var userId = GetUserId();
            var totalTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId)
                .CountAsync();

            var activeTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId && t.Status == TicketStatus.Active)
                .CountAsync();

            var totalSpent = await _context.LotteryTickets
                .Where(t => t.UserId == userId)
                .SumAsync(t => (decimal?)t.PurchasePrice) ?? 0;

            var analytics = new
            {
                totalTickets = totalTickets,
                activeTickets = activeTickets,
                totalSpent = totalSpent,
                averageTicketPrice = totalTickets > 0 ? totalSpent / totalTickets : 0
            };

            return Ok(new
            {
                success = true,
                data = analytics,
                message = "Ticket analytics retrieved successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { success = false, error = new { message = ex.Message } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ticket analytics");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching ticket analytics", details = ex.Message } });
        }
    }
}

