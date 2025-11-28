using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Caching;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HousesController : ControllerBase
    {
        private readonly LotteryDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<HousesController> _logger;
        private readonly ILotteryService _lotteryService;
        private readonly ICache? _cache;
        private static readonly TimeSpan HousesCacheExpiration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan HouseCacheExpiration = TimeSpan.FromMinutes(15);

        public HousesController(
            LotteryDbContext context, 
            IEventPublisher eventPublisher, 
            ILogger<HousesController> logger,
            ILotteryService lotteryService,
            ICache? cache = null)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _lotteryService = lotteryService;
            _cache = cache;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// Invalidates all house list caches when a house is created, updated, or deleted
        /// </summary>
        private async Task InvalidateHouseCachesAsync(Guid? houseId = null)
        {
            if (_cache != null)
            {
                try
                {
                    // Delete all cache keys matching the pattern "houses_*" (house lists)
                    var listResult = await _cache.DeleteByRegex("houses_*");
                    if (listResult)
                    {
                        _logger.LogDebug("Invalidated all house list caches");
                    }
                    
                    // If specific house ID provided, also invalidate individual house cache
                    if (houseId.HasValue)
                    {
                        var houseKey = $"house_{houseId.Value}";
                        await _cache.RemoveRecordAsync(houseKey);
                        _logger.LogDebug("Invalidated individual house cache for id: {HouseId}", houseId.Value);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't throw - cache invalidation failure shouldn't break the operation
                    _logger.LogWarning(ex, "Failed to invalidate house caches (non-critical)");
                }
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 1800, VaryByQueryKeys = new[] { "page", "limit", "status", "minPrice", "maxPrice", "location", "bedrooms", "bathrooms" })]
        public async Task<ActionResult<ApiResponse<PagedResponse<HouseDto>>>> GetHouses(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string? status = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? location = null,
            [FromQuery] int? bedrooms = null,
            [FromQuery] int? bathrooms = null)
        {
            try
            {
                // Generate cache key from query parameters (sanitize special characters)
                // Sanitize location to prevent cache key issues with special characters
                var sanitizedLocation = string.IsNullOrEmpty(location) 
                    ? "null" 
                    : System.Text.RegularExpressions.Regex.Replace(location, @"[^a-zA-Z0-9_-]", "_");
                
                var cacheKey = $"houses_{page}_{limit}_{status ?? "null"}_{minPrice?.ToString() ?? "null"}_{maxPrice?.ToString() ?? "null"}_{sanitizedLocation}_{bedrooms?.ToString() ?? "null"}_{bathrooms?.ToString() ?? "null"}";
                
                // Try to get from cache first (with error handling)
                if (_cache != null)
                {
                    try
                    {
                        var cachedResponse = await _cache.GetRecordAsync<PagedResponse<HouseDto>>(cacheKey);
                        if (cachedResponse != null)
                        {
                            _logger.LogDebug("Houses list retrieved from cache for key: {CacheKey}", cacheKey);
                            return Ok(new ApiResponse<PagedResponse<HouseDto>>
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
                        _logger.LogWarning(cacheEx, "Error retrieving from cache, falling back to database query");
                    }
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

                // Query ticket counts (caching would require complex key management for dynamic house lists)
                // Since house lists are already cached, ticket counts are queried fresh each time
                // This ensures data consistency while still benefiting from house list caching
                var houseIds = houses.Select(h => h.Id).ToList();
                var ticketCounts = await _context.LotteryTickets
                    .AsNoTracking()
                    .Where(t => houseIds.Contains(t.HouseId) && t.Status == "Active")
                    .GroupBy(t => t.HouseId)
                    .Select(g => new { HouseId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var houseDtos = houses.Select(house =>
                {
                    var ticketsSold = ticketCounts.FirstOrDefault(tc => tc.HouseId == house.Id)?.Count ?? 0;
                    var participationPercentage = house.TotalTickets > 0 ? (decimal)ticketsSold / house.TotalTickets * 100 : 0;
                    var canExecute = participationPercentage >= house.MinimumParticipationPercentage;

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
                if (_cache != null)
                {
                    try
                    {
                        await _cache.SetRecordAsync(cacheKey, pagedResponse, HousesCacheExpiration);
                        _logger.LogDebug("Houses list cached with key: {CacheKey}", cacheKey);
                    }
                    catch (Exception cacheEx)
                    {
                        // Log cache error but don't fail the request
                        _logger.LogWarning(cacheEx, "Error caching houses list, request still successful");
                    }
                }

                return Ok(new ApiResponse<PagedResponse<HouseDto>>
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
                return StatusCode(500, new ApiResponse<PagedResponse<HouseDto>>
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

        [HttpGet("{id}")]
        [ResponseCache(Duration = 900)] // 15 minutes
        public async Task<ActionResult<ApiResponse<HouseDto>>> GetHouse(Guid id)
        {
            try
            {
                var cacheKey = $"house_{id}";
                
                // Try to get from cache first (with error handling)
                if (_cache != null)
                {
                    try
                    {
                        var cachedResponse = await _cache.GetRecordAsync<HouseDto>(cacheKey);
                        if (cachedResponse != null)
                        {
                            _logger.LogDebug("House retrieved from cache for id: {HouseId}", id);
                            return Ok(new ApiResponse<HouseDto>
                            {
                                Success = true,
                                Data = cachedResponse,
                                Message = "House retrieved successfully (cached)"
                            });
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        // Log cache error but continue with database query
                        _logger.LogWarning(cacheEx, "Error retrieving house from cache, falling back to database query");
                    }
                }
                
                var house = await _context.Houses
                    .Include(h => h.Images)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (house == null)
                {
                    return NotFound(new ApiResponse<HouseDto>
                    {
                        Success = false,
                        Message = "House not found"
                    });
                }

                var ticketsSold = await _context.LotteryTickets
                    .CountAsync(t => t.HouseId == id && t.Status == "Active");

                var participationPercentage = house.TotalTickets > 0 ? (decimal)ticketsSold / house.TotalTickets * 100 : 0;
                var canExecute = participationPercentage >= house.MinimumParticipationPercentage;

                var houseDto = new HouseDto
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

                // Cache the response (with error handling)
                if (_cache != null)
                {
                    try
                    {
                        await _cache.SetRecordAsync(cacheKey, houseDto, HouseCacheExpiration);
                    }
                    catch (Exception cacheEx)
                    {
                        // Log cache error but don't fail the request
                        _logger.LogWarning(cacheEx, "Error caching house for id: {HouseId}", id);
                    }
                }

                return Ok(new ApiResponse<HouseDto>
                {
                    Success = true,
                    Data = houseDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving house {HouseId}", id);
                return StatusCode(500, new ApiResponse<HouseDto>
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

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<HouseDto>>> CreateHouse([FromBody] CreateHouseRequest request)
        {
            try
            {
                var house = new House
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    Description = request.Description,
                    Price = request.Price,
                    Location = request.Location,
                    Address = request.Address,
                    Bedrooms = request.Bedrooms,
                    Bathrooms = request.Bathrooms,
                    SquareFeet = request.SquareFeet,
                    PropertyType = request.PropertyType,
                    YearBuilt = request.YearBuilt,
                    LotSize = request.LotSize,
                    Features = request.Features,
                    Status = "Upcoming",
                    TotalTickets = request.TotalTickets,
                    TicketPrice = request.TicketPrice,
                    LotteryStartDate = request.LotteryStartDate,
                    LotteryEndDate = request.LotteryEndDate,
                    MinimumParticipationPercentage = request.MinimumParticipationPercentage,
                    MaxParticipants = request.MaxParticipants,
                    CreatedBy = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Houses.Add(house);
                await _context.SaveChangesAsync();

                await _eventPublisher.PublishAsync(new HouseCreatedEvent
                {
                    HouseId = house.Id,
                    Title = house.Title,
                    Price = house.Price,
                    CreatedByUserId = house.CreatedBy ?? Guid.Empty
                });

                // Invalidate house list caches (new house doesn't have individual cache yet)
                await InvalidateHouseCachesAsync();

                var houseDto = new HouseDto
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
                    TicketsSold = 0,
                    ParticipationPercentage = 0,
                    CanExecute = false,
                    Images = new List<HouseImageDto>(),
                    CreatedAt = house.CreatedAt
                };

                return Ok(new ApiResponse<HouseDto>
                {
                    Success = true,
                    Data = houseDto,
                    Message = "House created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating house");
                return StatusCode(500, new ApiResponse<HouseDto>
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

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<HouseDto>>> UpdateHouse(Guid id, [FromBody] UpdateHouseRequest request)
        {
            try
            {
                var house = await _context.Houses.FindAsync(id);

                if (house == null)
                {
                    return NotFound(new ApiResponse<HouseDto>
                    {
                        Success = false,
                        Message = "House not found"
                    });
                }

                if (!string.IsNullOrEmpty(request.Title))
                    house.Title = request.Title;

                if (request.Description != null)
                    house.Description = request.Description;

                if (request.Price.HasValue)
                    house.Price = request.Price.Value;

                if (!string.IsNullOrEmpty(request.Location))
                    house.Location = request.Location;

                if (request.Address != null)
                    house.Address = request.Address;

                if (request.Bedrooms.HasValue)
                    house.Bedrooms = request.Bedrooms.Value;

                if (request.Bathrooms.HasValue)
                    house.Bathrooms = request.Bathrooms.Value;

                if (request.SquareFeet.HasValue)
                    house.SquareFeet = request.SquareFeet.Value;

                if (request.PropertyType != null)
                    house.PropertyType = request.PropertyType;

                if (request.YearBuilt.HasValue)
                    house.YearBuilt = request.YearBuilt.Value;

                if (request.LotSize.HasValue)
                    house.LotSize = request.LotSize.Value;

                if (request.Features != null)
                    house.Features = request.Features;

                if (request.TotalTickets.HasValue)
                    house.TotalTickets = request.TotalTickets.Value;

                if (request.TicketPrice.HasValue)
                    house.TicketPrice = request.TicketPrice.Value;

                if (request.LotteryStartDate.HasValue)
                    house.LotteryStartDate = request.LotteryStartDate.Value;

                if (request.LotteryEndDate.HasValue)
                    house.LotteryEndDate = request.LotteryEndDate.Value;

                if (request.MinimumParticipationPercentage.HasValue)
                    house.MinimumParticipationPercentage = request.MinimumParticipationPercentage.Value;

                if (request.MaxParticipants.HasValue)
                {
                    // Validate max_participants > 0 if set
                    if (request.MaxParticipants.Value <= 0)
                    {
                        return BadRequest(new ApiResponse<HouseDto>
                        {
                            Success = false,
                            Message = "MaxParticipants must be greater than 0",
                            Error = new ErrorResponse
                            {
                                Code = "INVALID_INPUT",
                                Message = "MaxParticipants must be greater than 0"
                            }
                        });
                    }

                    // Check current participants count
                    var currentCount = await _lotteryService.GetParticipantCountAsync(id);
                    if (request.MaxParticipants.Value < currentCount)
                    {
                        return BadRequest(new ApiResponse<HouseDto>
                        {
                            Success = false,
                            Message = $"Cannot set max_participants ({request.MaxParticipants.Value}) less than current participants ({currentCount})",
                            Error = new ErrorResponse
                            {
                                Code = "INVALID_INPUT",
                                Message = $"Cannot set max_participants less than current participants ({currentCount})"
                            }
                        });
                    }

                    house.MaxParticipants = request.MaxParticipants.Value;
                }

                house.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _eventPublisher.PublishAsync(new HouseUpdatedEvent
                {
                    HouseId = house.Id,
                    Title = house.Title,
                    Price = house.Price
                });

                var ticketsSold = await _context.LotteryTickets
                    .CountAsync(t => t.HouseId == id && t.Status == "Active");

                var participationPercentage = house.TotalTickets > 0 ? (decimal)ticketsSold / house.TotalTickets * 100 : 0;
                var canExecute = participationPercentage >= house.MinimumParticipationPercentage;

                var houseDto = new HouseDto
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
                    Images = new List<HouseImageDto>(),
                    CreatedAt = house.CreatedAt
                };

                // Invalidate house list caches and individual house cache
                await InvalidateHouseCachesAsync(house.Id);

                return Ok(new ApiResponse<HouseDto>
                {
                    Success = true,
                    Data = houseDto,
                    Message = "House updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating house {HouseId}", id);
                return StatusCode(500, new ApiResponse<HouseDto>
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

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteHouse(Guid id)
        {
            try
            {
                var house = await _context.Houses.FindAsync(id);

                if (house == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "House not found"
                    });
                }

                house.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Invalidate house list caches and individual house cache
                await InvalidateHouseCachesAsync(id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "House deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting house {HouseId}", id);
                return StatusCode(500, new ApiResponse<object>
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

        /// <summary>
        /// Get user's favorite houses
        /// </summary>
        [HttpGet("favorites")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<HouseDto>>>> GetFavoriteHouses()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<List<HouseDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var favoriteHouses = await _lotteryService.GetUserFavoriteHousesAsync(userId.Value);

                return Ok(new ApiResponse<List<HouseDto>>
                {
                    Success = true,
                    Data = favoriteHouses,
                    Message = "Favorite houses retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorite houses");
                return StatusCode(500, new ApiResponse<List<HouseDto>>
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

        /// <summary>
        /// Add a house to user's favorites
        /// </summary>
        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<FavoriteHouseResponse>>> AddToFavorites(Guid id)
        {
            // #region agent log
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("[DEBUG] HousesController.AddToFavorites:entry houseId={HouseId} userId={UserId} hasUserId={HasUserId} lotteryServiceNull={Null}", id, currentUserId, currentUserId != null, _lotteryService == null);
            // #endregion
            try
            {
                var userId = GetCurrentUserId();
                // #region agent log
                _logger.LogInformation("[DEBUG] HousesController.AddToFavorites:after-get-user-id houseId={HouseId} userId={UserId} isNull={IsNull}", id, userId, userId == null);
                // #endregion
                if (userId == null)
                {
                    // #region agent log
                    _logger.LogWarning("[DEBUG] HousesController.AddToFavorites:unauthorized houseId={HouseId} userIdIsNull=true", id);
                    // #endregion
                    return Unauthorized(new ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // #region agent log
                _logger.LogInformation("[DEBUG] HousesController.AddToFavorites:before-service-call houseId={HouseId} userId={UserId} lotteryServiceType={Type}", id, userId ?? Guid.Empty, _lotteryService?.GetType().Name ?? "null");
                // #endregion
                var success = await _lotteryService.AddHouseToFavoritesAsync(userId!.Value, id);
                // #region agent log
                _logger.LogInformation("[DEBUG] HousesController.AddToFavorites:after-service-call houseId={HouseId} userId={UserId} success={Success} successType={Type}", id, userId.Value, success, success.GetType().Name);
                // #endregion

                if (!success)
                {
                    // #region agent log
                    _logger.LogWarning("[DEBUG] HousesController.AddToFavorites:service-returned-false houseId={HouseId} userId={UserId} - returning BadRequest", id, userId.Value);
                    // #endregion
                    return BadRequest(new ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "Failed to add house to favorites. House may not exist or already be in favorites."
                    });
                }

                // #region agent log
                _logger.LogInformation("[DEBUG] HousesController.AddToFavorites:success houseId={HouseId} userId={UserId} - returning Ok", id, userId.Value);
                // #endregion
                return Ok(new ApiResponse<FavoriteHouseResponse>
                {
                    Success = true,
                    Data = new FavoriteHouseResponse
                    {
                        HouseId = id,
                        Added = true,
                        Message = "House added to favorites successfully"
                    }
                });
            }
            catch (Exception ex)
            {
                // #region agent log
                _logger.LogError(ex, "[DEBUG] HousesController.AddToFavorites:exception houseId={HouseId} exceptionType={Type} message={Message} stackTrace={StackTrace}", id, ex.GetType().Name, ex.Message, ex.StackTrace?.Substring(0, Math.Min(1000, ex.StackTrace?.Length ?? 0)));
                // #endregion
                _logger.LogError(ex, "Error adding house {HouseId} to favorites", id);
                return StatusCode(500, new ApiResponse<FavoriteHouseResponse>
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

        /// <summary>
        /// Remove a house from user's favorites
        /// </summary>
        [HttpDelete("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<FavoriteHouseResponse>>> RemoveFromFavorites(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var success = await _lotteryService.RemoveHouseFromFavoritesAsync(userId.Value, id);

                if (!success)
                {
                    return BadRequest(new ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "Failed to remove house from favorites. House may not be in favorites."
                    });
                }

                return Ok(new ApiResponse<FavoriteHouseResponse>
                {
                    Success = true,
                    Data = new FavoriteHouseResponse
                    {
                        HouseId = id,
                        Added = false,
                        Message = "House removed from favorites successfully"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing house {HouseId} from favorites", id);
                return StatusCode(500, new ApiResponse<FavoriteHouseResponse>
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

        /// <summary>
        /// Get recommended houses for the user
        /// </summary>
        [HttpGet("recommendations")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<RecommendedHouseDto>>>> GetRecommendedHouses([FromQuery] int limit = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<List<RecommendedHouseDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                if (limit < 1 || limit > 50)
                {
                    limit = 10;
                }

                var recommendedHouses = await _lotteryService.GetRecommendedHousesAsync(userId.Value, limit);

                // Convert to RecommendedHouseDto with scores and reasons
                var recommendedDtos = recommendedHouses.Select((house, index) => new RecommendedHouseDto
                {
                    // Copy all HouseDto properties
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
                    TicketsSold = house.TicketsSold,
                    ParticipationPercentage = house.ParticipationPercentage,
                    CanExecute = house.CanExecute,
                    Images = house.Images,
                    CreatedAt = house.CreatedAt,
                    // Add recommendation-specific fields
                    RecommendationScore = Math.Round(0.9m - (index * 0.1m), 2),
                    Reason = index == 0 ? "Based on your favorites" : "Similar to your preferences"
                }).ToList();

                return Ok(new ApiResponse<List<RecommendedHouseDto>>
                {
                    Success = true,
                    Data = recommendedDtos,
                    Message = "Recommended houses retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommended houses");
                return StatusCode(500, new ApiResponse<List<RecommendedHouseDto>>
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

        /// <summary>
        /// Get participant statistics for a house
        /// GET /api/v1/houses/{id}/participants
        /// </summary>
        [HttpGet("{id}/participants")]
        public async Task<ActionResult<ApiResponse<LotteryParticipantStatsDto>>> GetParticipantStats(Guid id)
        {
            try
            {
                var stats = await _lotteryService.GetParticipantStatsAsync(id);

                return Ok(new ApiResponse<LotteryParticipantStatsDto>
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<LotteryParticipantStatsDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving participant stats for house {HouseId}", id);
                return StatusCode(500, new ApiResponse<LotteryParticipantStatsDto>
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

        /// <summary>
        /// Check if current user can enter lottery
        /// GET /api/v1/houses/{id}/can-enter
        /// </summary>
        [HttpGet("{id}/can-enter")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CanEnterLotteryResponse>>> CanEnterLottery(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<CanEnterLotteryResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var canEnter = await _lotteryService.CanUserEnterLotteryAsync(userId, id);
                
                // Check if user is existing participant
                var isExistingParticipant = await _context.LotteryTickets
                    .AnyAsync(t => t.HouseId == id 
                        && t.UserId == userId 
                        && t.Status == "Active");

                var response = new CanEnterLotteryResponse
                {
                    CanEnter = canEnter,
                    IsExistingParticipant = isExistingParticipant,
                    Reason = canEnter ? null : "PARTICIPANT_CAP_REACHED"
                };

                return Ok(new ApiResponse<CanEnterLotteryResponse>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can enter lottery for house {HouseId}", id);
                return StatusCode(500, new ApiResponse<CanEnterLotteryResponse>
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

