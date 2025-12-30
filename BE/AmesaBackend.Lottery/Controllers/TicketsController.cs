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
        // #region agent log
        _logger.LogInformation("[DEBUG] GetActiveTickets entry - contextNull={ContextNull}", _context == null);
        // #endregion
        
        if (_context == null)
        {
            _logger.LogError("LotteryDbContext is not available");
            return StatusCode(503, new { success = false, error = new { message = "Database service is not available" } });
        }
        
        try
        {
            var userId = GetUserId();
            // #region agent log
            _logger.LogInformation("[DEBUG] Before database query - userId={UserId}", userId);
            // #endregion
            var activeTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId && t.Status == TicketStatus.Active)
                .Include(t => t.House)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            // #region agent log
            _logger.LogInformation("[DEBUG] After database query - count={Count}", activeTickets?.Count ?? -1);
            // #endregion

            return Ok(new
            {
                success = true,
                data = activeTickets ?? new List<object>(),
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
            // #region agent log
            _logger.LogError(ex, "[DEBUG] Exception in GetActiveTickets - Type={ExceptionType}, Message={Message}, StackTrace={StackTrace}", 
                ex.GetType().FullName, ex.Message, ex.StackTrace);
            // #endregion
            _logger.LogError(ex, "Error fetching active tickets - InnerException: {InnerException}", ex.InnerException?.Message);
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching active tickets", details = ex.Message } });
        }
    }

    /// <summary>
    /// Get ticket analytics for the current user
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult> GetTicketAnalytics()
    {
        // #region agent log
        _logger.LogInformation("[DEBUG] GetTicketAnalytics entry - contextNull={ContextNull}", _context == null);
        // #endregion
        
        if (_context == null)
        {
            _logger.LogError("LotteryDbContext is not available");
            return StatusCode(503, new { success = false, error = new { message = "Database service is not available" } });
        }
        
        try
        {
            var userId = GetUserId();
            // #region agent log
            _logger.LogInformation("[DEBUG] Before CountAsync - userId={UserId}", userId);
            // #endregion
            var totalTickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId)
                .CountAsync();
            // #region agent log
            _logger.LogInformation("[DEBUG] After CountAsync - totalTickets={TotalTickets}", totalTickets);
            // #endregion

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
            // #region agent log
            _logger.LogError(ex, "[DEBUG] Exception in GetTicketAnalytics - Type={ExceptionType}, Message={Message}, StackTrace={StackTrace}", 
                ex.GetType().Name, ex.Message, ex.StackTrace);
            // #endregion
            _logger.LogError(ex, "Error fetching ticket analytics");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching ticket analytics", details = ex.Message } });
        }
    }
}

