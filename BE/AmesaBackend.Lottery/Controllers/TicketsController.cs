using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers;

[ApiController]
[Route("api/v1/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly LotteryDbContext _context;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        LotteryDbContext context,
        ILogger<TicketsController> logger)
    {
        _context = context;
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
    /// Get active tickets for the current user
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult> GetActiveTickets()
    {
        try
        {
            var userId = GetUserId();

            var activeTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId && t.Status == "Active")
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
            _logger.LogError(ex, "Error fetching active tickets");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching active tickets" } });
        }
    }

    /// <summary>
    /// Get ticket analytics for the current user
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult> GetTicketAnalytics()
    {
        try
        {
            var userId = GetUserId();

            var totalTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId)
                .CountAsync();

            var activeTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId && t.Status == "Active")
                .CountAsync();

            var totalSpent = await _context.LotteryTickets
                .Where(t => t.UserId == userId)
                .SumAsync(t => t.Price);

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
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching ticket analytics" } });
        }
    }
}

