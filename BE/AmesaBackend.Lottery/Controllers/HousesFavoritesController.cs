using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Auth.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace AmesaBackend.Lottery.Controllers;

[ApiController]
[Route("api/v1/houses")]
[Authorize]
public class HousesFavoritesController : ControllerBase
{
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly LotteryDbContext _context;
    private readonly ILogger<HousesFavoritesController> _logger;

    public HousesFavoritesController(
        IUserPreferencesService userPreferencesService,
        LotteryDbContext context,
        ILogger<HousesFavoritesController> logger)
    {
        _userPreferencesService = userPreferencesService;
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
    /// Get user's favorite houses
    /// </summary>
    [HttpGet("favorites")]
    public async Task<ActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? sortBy = null, [FromQuery] string? sortOrder = "asc")
    {
        // #region agent log
        try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,C,D", location = "HousesFavoritesController.cs:49", message = "GetFavorites entry", data = new { page, limit, sortBy, sortOrder }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        try
        {
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "E", location = "HousesFavoritesController.cs:53", message = "Before GetUserId", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            var userId = GetUserId();
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "E", location = "HousesFavoritesController.cs:55", message = "After GetUserId", data = new { userId = userId.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,D", location = "HousesFavoritesController.cs:56", message = "Before GetFavoriteHouseIdsAsync", data = new { userId = userId.ToString(), serviceNull = _userPreferencesService == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            var favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId);
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "HousesFavoritesController.cs:58", message = "After GetFavoriteHouseIdsAsync", data = new { count = favoriteHouseIds?.Count ?? -1, isNull = favoriteHouseIds == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion

            if (favoriteHouseIds == null || !favoriteHouseIds.Any())
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items = new List<object>(),
                        totalCount = 0,
                        page = page,
                        limit = limit,
                        totalPages = 0
                    },
                    message = "No favorite houses found"
                });
            }

            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "HousesFavoritesController.cs:75", message = "Before database query", data = new { contextNull = _context == null, favoriteCount = favoriteHouseIds?.Count ?? 0 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            var query = _context.Houses
                .Where(h => favoriteHouseIds.Contains(h.Id) && h.Status == "Active");

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                query = sortBy.ToLower() switch
                {
                    "dateadded" => sortOrder?.ToLower() == "desc" 
                        ? query.OrderByDescending(h => h.CreatedAt)
                        : query.OrderBy(h => h.CreatedAt),
                    "price" => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(h => h.Price)
                        : query.OrderBy(h => h.Price),
                    _ => query.OrderBy(h => h.CreatedAt)
                };
            }
            else
            {
                query = query.OrderBy(h => h.CreatedAt);
            }

            var totalCount = await query.CountAsync();
            var houses = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    items = houses,
                    totalCount = totalCount,
                    page = page,
                    limit = limit,
                    totalPages = (int)Math.Ceiling(totalCount / (double)limit)
                },
                message = "Favorite houses retrieved successfully"
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
            try { await System.IO.File.AppendAllTextAsync(@"c:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B,C,D,E", location = "HousesFavoritesController.cs:123", message = "Exception caught", data = new { exceptionType = ex.GetType().Name, message = ex.Message, stackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length ?? 0)) }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            _logger.LogError(ex, "Error fetching favorite houses");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching favorite houses" } });
        }
    }

    /// <summary>
    /// Get count of user's favorite houses
    /// </summary>
    [HttpGet("favorites/count")]
    public async Task<ActionResult> GetFavoritesCount()
    {
        try
        {
            var userId = GetUserId();
            var favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId);

            var count = favoriteHouseIds?.Count ?? 0;

            return Ok(new
            {
                success = true,
                data = new { count = count },
                message = "Favorite houses count retrieved successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { success = false, error = new { message = ex.Message } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching favorites count");
            return StatusCode(500, new { success = false, error = new { message = "An error occurred while fetching favorites count" } });
        }
    }
}

