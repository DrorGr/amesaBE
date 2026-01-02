using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Services.Interfaces;
using AmesaBackend.Shared.Caching;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/houses")]
    public class HousesSearchController : ControllerBase
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<HousesSearchController> _logger;
        private readonly ICache _cache;
        private readonly ILotteryService? _lotteryService;
        private static readonly TimeSpan HousesCacheExpiration = TimeSpan.FromMinutes(30);

        public HousesSearchController(
            LotteryDbContext context,
            ILogger<HousesSearchController> logger,
            ICache cache,
            ILotteryService? lotteryService = null)
        {
            _context = context;
            _logger = logger;
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _lotteryService = lotteryService;
        }

        [HttpGet]
        [ResponseCache(Duration = 1800, VaryByQueryKeys = new[] { "page", "limit", "status", "minPrice", "maxPrice", "location", "bedrooms", "bathrooms" })]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>>> GetHouses(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string? status = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? location = null,
            [FromQuery] int? bedrooms = null,
            [FromQuery] int? bathrooms = null)
        {
            // #endregion
            try
            {
                // Generate cache key from query parameters (sanitize null values)
                var cacheKey = $"houses_{page}_{limit}_{status ?? "null"}_{minPrice?.ToString() ?? "null"}_{maxPrice?.ToString() ?? "null"}_{(location ?? "null").Replace(" ", "_")}_{bedrooms?.ToString() ?? "null"}_{bathrooms?.ToString() ?? "null"}";
                
                // Try to get from cache first (with error handling)
                try
                {
                    var cachedResponse = await _cache.GetRecordAsync<PagedResponse<HouseDto>>(cacheKey);
                    if (cachedResponse != null)
                    {
                        _logger.LogDebug("Houses list retrieved from cache for key: {CacheKey}", cacheKey);
                        return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                        {
                            Success = true,
                            Data = cachedResponse,
                            Message = "Houses retrieved successfully (cached)"
                        });
                    }
                }
                catch (Exception cacheEx)
                {
                    // Log cache error but continue with database query
                    // Fail-open design (matches Auth service pattern)
                    _logger.LogWarning(cacheEx, "Error retrieving from cache, falling back to database query");
                }
                
                var query = _context.Houses
                    .Include(h => h.Images)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(h => h.Status == status);
                }

                if (minPrice.HasValue)
                {
                    query = query.Where(h => h.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(h => h.Price <= maxPrice.Value);
                }

                if (!string.IsNullOrEmpty(location))
                {
                    query = query.Where(h => h.Location.Contains(location));
                }

                if (bedrooms.HasValue)
                {
                    query = query.Where(h => h.Bedrooms == bedrooms.Value);
                }

                if (bathrooms.HasValue)
                {
                    query = query.Where(h => h.Bathrooms == bathrooms.Value);
                }

                var totalCount = await query.CountAsync();

                var houses = await query
                    .AsNoTracking()
                    .OrderByDescending(h => h.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                // OPTIMIZED: Combined query for ticket counts and unique participants
                // This reduces database round-trips from 2 to 1, improving performance by ~2x
                var houseIds = houses.Select(h => h.Id).ToList();
                var ticketStats = await _context.LotteryTickets
                    .AsNoTracking()
                    .Where(t => houseIds.Contains(t.HouseId) && t.Status == "Active")
                    .GroupBy(t => t.HouseId)
                    .Select(g => new 
                    { 
                        HouseId = g.Key, 
                        TicketCount = g.Count(),
                        UniqueParticipants = g.Select(t => t.UserId).Distinct().Count()
                    })
                    .ToListAsync();

                // Batch fetch ProductIds for all houses in parallel (non-critical, don't fail if it fails)
                Dictionary<Guid, Guid?> productIds = new Dictionary<Guid, Guid?>();
                if (_lotteryService != null)
                {
                    try
                    {
                        productIds = await _lotteryService.GetProductIdsForHousesAsync(houseIds);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not batch fetch product IDs for houses (non-critical)");
                    }
                }

                var houseDtos = houses.Select(house =>
                {
                    // Get ticket statistics from combined query
                    var stats = ticketStats.FirstOrDefault(ts => ts.HouseId == house.Id);
                    var ticketsSold = stats?.TicketCount ?? 0;
                    var participationPercentage = house.TotalTickets > 0 ? (decimal)ticketsSold / house.TotalTickets * 100 : 0;
                    var canExecute = participationPercentage >= house.MinimumParticipationPercentage;

                    // Get unique participants count from combined query
                    var uniqueParticipants = stats?.UniqueParticipants ?? 0;
                    var isCapReached = house.MaxParticipants.HasValue 
                        && uniqueParticipants >= house.MaxParticipants.Value;
                    var remainingSlots = house.MaxParticipants.HasValue
                        ? Math.Max(0, house.MaxParticipants.Value - uniqueParticipants)
                        : (int?)null;

                    return new HouseDto
                    {
                        Id = house.Id,
                        Title = house.Title,
                        Description = house.Description,
                        Price = house.Price,
                        Location = house.Location,
                        Address = house.Address,
                        Bedrooms = house.Bedrooms,
                        Bathrooms = house.Bathrooms,
                        SquareFeet = house.SquareFeet,
                        PropertyType = house.PropertyType,
                        YearBuilt = house.YearBuilt,
                        LotSize = house.LotSize,
                        Features = house.Features,
                        Status = house.Status,
                        TotalTickets = house.TotalTickets,
                        TicketPrice = house.TicketPrice,
                        LotteryStartDate = house.LotteryStartDate,
                        LotteryEndDate = house.LotteryEndDate,
                        DrawDate = house.DrawDate,
                        MinimumParticipationPercentage = house.MinimumParticipationPercentage,
                        TicketsSold = ticketsSold,
                        ParticipationPercentage = Math.Round(participationPercentage, 2),
                        CanExecute = canExecute,
                        MaxParticipants = house.MaxParticipants,
                        UniqueParticipants = uniqueParticipants,
                        IsParticipantCapReached = isCapReached,
                        RemainingParticipantSlots = remainingSlots,
                        ProductId = productIds.GetValueOrDefault(house.Id), // Batch fetched ProductId for payment integration
                        Images = house.Images.OrderBy(i => i.DisplayOrder).Select(i => new HouseImageDto
                        {
                            Id = i.Id,
                            ImageUrl = i.ImageUrl,
                            AltText = i.AltText,
                            DisplayOrder = i.DisplayOrder,
                            IsPrimary = i.IsPrimary,
                            MediaType = i.MediaType,
                            FileSize = i.FileSize,
                            Width = i.Width,
                            Height = i.Height
                        }).ToList(),
                        CreatedAt = house.CreatedAt
                    };
                }).ToList();

                var pagedResponse = new PagedResponse<HouseDto>
                {
                    Items = houseDtos,
                    Page = page,
                    Limit = limit,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / limit),
                    HasNext = page * limit < totalCount,
                    HasPrevious = page > 1
                };

                // Cache the response (with error handling)
                try
                {
                    await _cache.SetRecordAsync(cacheKey, pagedResponse, HousesCacheExpiration);
                    _logger.LogDebug("Houses list cached with key: {CacheKey}", cacheKey);
                }
                catch (Exception cacheEx)
                {
                    // Log cache error but don't fail the request
                    // Fail-open design (matches Auth service pattern)
                    _logger.LogWarning(cacheEx, "Error caching houses list, request still successful");
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                {
                    Success = true,
                    Data = pagedResponse,
                    Message = "Houses retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching houses: {Message}", ex.Message);
                _logger.LogError(ex, "Error retrieving houses");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }
    }
}


