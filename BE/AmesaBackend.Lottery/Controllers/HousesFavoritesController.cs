using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace AmesaBackend.Lottery.Controllers;

/// <summary>
/// Controller for managing user favorite houses in the lottery system.
/// Provides endpoints for retrieving and managing a user's list of favorite lottery houses.
/// </summary>
[ApiController]
[Route("api/v1/houses")]
[Authorize]
public class HousesFavoritesController : ControllerBase
{
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly LotteryDbContext _context;
    private readonly ILogger<HousesFavoritesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HousesFavoritesController"/> class.
    /// </summary>
    /// <param name="userPreferencesService">Service for managing user preferences including favorites.</param>
    /// <param name="context">Database context for lottery data access.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public HousesFavoritesController(
        IUserPreferencesService userPreferencesService,
        LotteryDbContext context,
        ILogger<HousesFavoritesController> logger)
    {
        _userPreferencesService = userPreferencesService;
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
    /// Retrieves a paginated list of the authenticated user's favorite houses.
    /// </summary>
    /// <param name="page">Page number (1-based). Must be greater than 0. Default is 1.</param>
    /// <param name="limit">Number of items per page. Must be between 1 and 100. Default is 20.</param>
    /// <param name="sortBy">Field to sort by. Supported values: "dateadded", "price". Default is by creation date.</param>
    /// <param name="sortOrder">Sort order. Supported values: "asc", "desc". Default is "asc".</param>
    /// <returns>
    /// A response containing:
    /// - success: Boolean indicating operation success
    /// - data: Object containing items (houses), totalCount, page, limit, totalPages
    /// - message: Success message
    /// </returns>
    /// <response code="200">Successfully retrieved favorite houses.</response>
    /// <response code="400">Invalid pagination parameters provided.</response>
    /// <response code="401">User is not authenticated or token is invalid.</response>
    /// <response code="500">An error occurred while processing the request.</response>
    [HttpGet("favorites")]
    public async Task<ActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? sortBy = null, [FromQuery] string? sortOrder = "asc")
    {
        // Validate pagination parameters
        if (page < 1)
        {
            return BadRequest(new { success = false, error = new { message = "Page must be greater than 0" } });
        }
        
        const int maxLimit = 100;
        if (limit > maxLimit)
        {
            return BadRequest(new { success = false, error = new { message = $"Maximum limit is {maxLimit}" } });
        }
        
        if (limit < 1)
        {
            return BadRequest(new { success = false, error = new { message = "Limit must be greater than 0" } });
        }
        
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
                .Where(h => favoriteHouseIds.Contains(h.Id) && h.Status == LotteryStatus.Active);

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
    /// Retrieves the total count of favorite houses for the authenticated user.
    /// </summary>
    /// <returns>
    /// A response containing:
    /// - success: Boolean indicating operation success
    /// - data: Object containing count (number of favorite houses)
    /// - message: Success message
    /// </returns>
    /// <response code="200">Successfully retrieved favorite houses count.</response>
    /// <response code="401">User is not authenticated or token is invalid.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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

