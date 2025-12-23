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
        try
        {
            var userId = GetUserId();
            var favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId);

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

