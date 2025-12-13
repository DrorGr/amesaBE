using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Shared.Helpers;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/houses")]
    public class HousesController : ControllerBase
    {
        private readonly LotteryDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<HousesController> _logger;
        private readonly ILotteryService _lotteryService;
        private readonly IPromotionService? _promotionService;
        private readonly ICache _cache;
        private readonly ITicketReservationService? _reservationService;
        private readonly IRedisInventoryManager? _inventoryManager;

        public HousesController(
            LotteryDbContext context, 
            IEventPublisher eventPublisher, 
            ILogger<HousesController> logger,
            ILotteryService lotteryService,
            ICache cache,
            IPromotionService? promotionService = null,
            ITicketReservationService? reservationService = null,
            IRedisInventoryManager? inventoryManager = null)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _lotteryService = lotteryService;
            _promotionService = promotionService;
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _reservationService = reservationService;
            _inventoryManager = inventoryManager;
        }

        [HttpGet("{id}")]
        [ResponseCache(Duration = 900)] // 15 minutes
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>>> GetHouse(Guid id)
        {
            // #region agent log
            _logger.LogInformation("[DEBUG_ROUTING] HousesController.GetHouse called - Method: {Method}, Path: {Path}, Id: {Id}", 
                HttpContext.Request.Method, 
                HttpContext.Request.Path, 
                id);
            // #endregion
            try
            {
                var house = await _context.Houses
                    .Include(h => h.Images)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (house == null)
                {
                    return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
                    {
                        Success = false,
                        Message = "House not found"
                    });
                }

                var ticketsSold = await _context.LotteryTickets
                    .CountAsync(t => t.HouseId == id && t.Status == "Active");

                var participationPercentage = house.TotalTickets > 0 ? (decimal)ticketsSold / house.TotalTickets * 100 : 0;
                var canExecute = participationPercentage >= house.MinimumParticipationPercentage;

                // Get unique participants count
                var uniqueParticipants = await _context.LotteryTickets
                    .Where(t => t.HouseId == id && t.Status == "Active")
                    .Select(t => t.UserId)
                    .Distinct()
                    .CountAsync();

                var isCapReached = house.MaxParticipants.HasValue 
                    && uniqueParticipants >= house.MaxParticipants.Value;
                var remainingSlots = house.MaxParticipants.HasValue
                    ? Math.Max(0, house.MaxParticipants.Value - uniqueParticipants)
                    : (int?)null;

                // Fetch product ID from Payment service (non-critical, don't fail if it fails)
                Guid? productId = null;
                try
                {
                    productId = await _lotteryService.GetProductIdForHouseAsync(house.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not fetch product ID for house {HouseId} (non-critical)", house.Id);
                }

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
                    MaxParticipants = house.MaxParticipants,
                    UniqueParticipants = uniqueParticipants,
                    IsParticipantCapReached = isCapReached,
                    RemainingParticipantSlots = remainingSlots,
                    ProductId = productId,
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

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
                {
                    Success = true,
                    Data = houseDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
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
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>>> CreateHouse([FromBody] CreateHouseRequest request)
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
                
                // Use transaction to ensure atomicity
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                // Create product for house in Payment service (non-critical, don't fail if it fails)
                Guid? productId = null;
                try
                {
                    productId = await _lotteryService.CreateProductForHouseAsync(
                        house.Id, 
                        house.Title, 
                        house.TicketPrice, 
                        house.CreatedBy);
                    
                    if (productId.HasValue)
                    {
                        _logger.LogInformation("Created product {ProductId} for house {HouseId}", productId.Value, house.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Product creation returned null for house {HouseId}", house.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create product for house {HouseId} (non-critical)", house.Id);
                    // Continue - product creation failure shouldn't block house creation
                }

                // Publish event after transaction commits (non-critical, don't fail if it fails)
                try
                {
                    await _eventPublisher.PublishAsync(new HouseCreatedEvent
                    {
                        HouseId = house.Id,
                        Title = house.Title,
                        Price = house.Price,
                        CreatedByUserId = house.CreatedBy ?? Guid.Empty
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish HouseCreatedEvent for house {HouseId}", house.Id);
                }

                // Invalidate house list caches
                try
                {
                    await _cache.DeleteByRegex("houses_*");
                    _logger.LogDebug("Invalidated all house list caches");
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the request
                    // Cache invalidation is non-critical - worst case, stale data is served briefly
                    _logger.LogWarning(ex, "Error invalidating house caches (non-critical)");
                }

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
                    MaxParticipants = house.MaxParticipants,
                    UniqueParticipants = 0,
                    IsParticipantCapReached = false,
                    RemainingParticipantSlots = house.MaxParticipants,
                    ProductId = productId,
                    Images = new List<HouseImageDto>(),
                    CreatedAt = house.CreatedAt
                };

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
                {
                    Success = true,
                    Data = houseDto,
                    Message = "House created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating house");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
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
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>>> UpdateHouse(Guid id, [FromBody] UpdateHouseRequest request)
        {
            try
            {
                var house = await _context.Houses.FindAsync(id);

                if (house == null)
                {
                    return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
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
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
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
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
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

                // Use transaction to ensure atomicity
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                // Publish event after transaction commits (non-critical, don't fail if it fails)
                try
                {
                    await _eventPublisher.PublishAsync(new HouseUpdatedEvent
                    {
                        HouseId = house.Id,
                        Title = house.Title,
                        Price = house.Price
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish HouseUpdatedEvent for house {HouseId}", house.Id);
                }

                var ticketsSold = await _context.LotteryTickets
                    .CountAsync(t => t.HouseId == id && t.Status == "Active");

                var participationPercentage = house.TotalTickets > 0 ? (decimal)ticketsSold / house.TotalTickets * 100 : 0;
                var canExecute = participationPercentage >= house.MinimumParticipationPercentage;

                // Get unique participants count
                var uniqueParticipants = await _context.LotteryTickets
                    .Where(t => t.HouseId == id && t.Status == "Active")
                    .Select(t => t.UserId)
                    .Distinct()
                    .CountAsync();

                var isCapReached = house.MaxParticipants.HasValue 
                    && uniqueParticipants >= house.MaxParticipants.Value;
                var remainingSlots = house.MaxParticipants.HasValue
                    ? Math.Max(0, house.MaxParticipants.Value - uniqueParticipants)
                    : (int?)null;

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
                    MaxParticipants = house.MaxParticipants,
                    UniqueParticipants = uniqueParticipants,
                    IsParticipantCapReached = isCapReached,
                    RemainingParticipantSlots = remainingSlots,
                    ProductId = null, // Product ID not fetched in update view for performance
                    Images = new List<HouseImageDto>(),
                    CreatedAt = house.CreatedAt
                };

                // Invalidate house list caches
                try
                {
                    await _cache.DeleteByRegex("houses_*");
                    _logger.LogDebug("Invalidated all house list caches");
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the request
                    // Cache invalidation is non-critical - worst case, stale data is served briefly
                    _logger.LogWarning(ex, "Error invalidating house caches (non-critical)");
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
                {
                    Success = true,
                    Data = houseDto,
                    Message = "House updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<HouseDto>
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
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<object>>> DeleteHouse(Guid id)
        {
            try
            {
                var house = await _context.Houses.FindAsync(id);

                if (house == null)
                {
                    return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                    {
                        Success = false,
                        Message = "House not found"
                    });
                }

                house.DeletedAt = DateTime.UtcNow;
                
                // Use transaction to ensure atomicity
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                // Invalidate house list caches (non-critical, don't fail if it fails)
                try
                {
                    await _cache.DeleteByRegex("houses_*");
                    _logger.LogDebug("Invalidated all house list caches");
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the request
                    // Cache invalidation is non-critical - worst case, stale data is served briefly
                    _logger.LogWarning(ex, "Error invalidating house caches (non-critical)");
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = true,
                    Message = "House deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<object>
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

        [HttpGet("{id}/inventory")]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<InventoryStatus>>> GetInventory(Guid id)
        {
            // Prevent caching - inventory must be real-time
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");
            
            try
            {
                if (_inventoryManager == null)
                {
                    return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<InventoryStatus>
                    {
                        Success = false,
                        Message = "Inventory service not available"
                    });
                }

                var inventory = await _inventoryManager.GetInventoryStatusAsync(id);
                
                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<InventoryStatus>
                {
                    Success = true,
                    Data = inventory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory for house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<InventoryStatus>
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

        [HttpPost("{id}/tickets/reserve")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>>> ReserveTickets(
            Guid id,
            [FromBody] CreateReservationRequest request)
        {
            try
            {
                if (_reservationService == null)
                {
                    return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                    {
                        Success = false,
                        Message = "Reservation service not available"
                    });
                }

                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                    {
                        Success = false,
                        Message = "Authentication required"
                    });
                }
                
                var reservation = await _reservationService.CreateReservationAsync(request, id, userId);

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                {
                    Success = true,
                    Data = reservation
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INVALID_OPERATION",
                        Message = ex.Message
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "UNAUTHORIZED",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving tickets for house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred creating your reservation. Please try again."
                    }
                });
            }
        }

        /// <summary>
        /// Validate ticket purchase before payment (called by Payment service)
        /// POST /api/v1/houses/{id}/tickets/validate
        /// </summary>
        [HttpPost("{id}/tickets/validate")]
        [AllowAnonymous]  // Service-to-service endpoint - authenticated via ServiceToServiceAuthMiddleware
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<ValidateTicketsResponse>>> ValidateTickets(
            Guid id,
            [FromBody] ValidateTicketsRequest request)
        {
            try
            {
                // Ensure houseId in path matches request
                if (request.HouseId != id)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<ValidateTicketsResponse>
                    {
                        Success = false,
                        Message = "House ID in path does not match request body"
                    });
                }

                var result = await _lotteryService.ValidateTicketsAsync(request);

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<ValidateTicketsResponse>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tickets for house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<ValidateTicketsResponse>
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
        /// Create tickets from payment transaction (called by Payment service after payment success)
        /// POST /api/v1/houses/{id}/tickets/create-from-payment
        /// </summary>
        [HttpPost("{id}/tickets/create-from-payment")]
        [AllowAnonymous]  // Service-to-service endpoint - authenticated via ServiceToServiceAuthMiddleware
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<CreateTicketsFromPaymentResponse>>> CreateTicketsFromPayment(
            Guid id,
            [FromBody] CreateTicketsFromPaymentRequest request)
        {
            try
            {
                // Ensure houseId in path matches request
                if (request.HouseId != id)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<CreateTicketsFromPaymentResponse>
                    {
                        Success = false,
                        Message = "House ID in path does not match request body"
                    });
                }

                var result = await _lotteryService.CreateTicketsFromPaymentAsync(request);

                // Publish event for real-time updates
                await _eventPublisher.PublishAsync(new TicketPurchasedEvent
                {
                    UserId = request.UserId,
                    HouseId = request.HouseId,
                    TicketCount = result.TicketsPurchased,
                    TicketNumbers = result.TicketNumbers
                });

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<CreateTicketsFromPaymentResponse>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating tickets from payment for house {HouseId}", id);
                return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<CreateTicketsFromPaymentResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INVALID_OPERATION",
                        Message = ex.Message
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Authorization error creating tickets from payment for house {HouseId}", id);
                return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<CreateTicketsFromPaymentResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "UNAUTHORIZED",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tickets from payment for house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<CreateTicketsFromPaymentResponse>
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
        /// Purchase tickets for a house (standard purchase endpoint)
        /// POST /api/v1/houses/{id}/tickets/purchase
        /// </summary>
        [HttpPost("{id}/tickets/purchase")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>>> PurchaseTickets(
            Guid id,
            [FromBody] PurchaseTicketsRequest request)
        {
            string? reservationToken = null;
            
            try
            {
                // Get user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Get house (read-only check - use AsNoTracking for performance)
                var house = await _context.Houses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id);
                if (house == null)
                {
                    return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                    {
                        Success = false,
                        Message = "House not found"
                    });
                }

                // Check ID verification requirement
                await _lotteryService.CheckVerificationRequirementAsync(userId);

                // Check participant cap
                var canEnter = await _lotteryService.CanUserEnterLotteryAsync(userId, id);
                if (!canEnter)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                    {
                        Success = false,
                        Message = "Participant cap reached. Cannot enter this lottery.",
                        Error = new ErrorResponse
                        {
                            Code = "PARTICIPANT_CAP_REACHED",
                            Message = "The maximum number of participants for this lottery has been reached."
                        }
                    });
                }

                // NEW: Check inventory availability and reserve tickets
                if (_inventoryManager != null)
                {
                    var inventory = await _inventoryManager.GetInventoryStatusAsync(id);
                    
                    if (inventory.AvailableTickets < request.Quantity)
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                        {
                            Success = false,
                            Message = $"Insufficient tickets available. Only {inventory.AvailableTickets} tickets remaining.",
                            Error = new ErrorResponse
                            {
                                Code = "INSUFFICIENT_TICKETS",
                                Message = $"Only {inventory.AvailableTickets} tickets available, but {request.Quantity} requested."
                            }
                        });
                    }
                    
                    // Reserve tickets with temporary hold
                    reservationToken = Guid.NewGuid().ToString();
                    var reserved = await _inventoryManager.ReserveInventoryAsync(id, request.Quantity, reservationToken);
                    
                    if (!reserved)
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                        {
                            Success = false,
                            Message = "Failed to reserve tickets. Please try again.",
                            Error = new ErrorResponse
                            {
                                Code = "RESERVATION_FAILED",
                                Message = "Could not reserve tickets. They may have been purchased by another user."
                            }
                        });
                    }
                    
                    _logger.LogInformation(
                        "Reserved {Quantity} tickets for house {HouseId} with reservation token {ReservationToken}",
                        request.Quantity, id, reservationToken);
                }

                // Calculate total cost
                var originalTotalCost = house.TicketPrice * request.Quantity;
                var totalCost = originalTotalCost;
                decimal discountAmount = 0;
                Guid? appliedPromotionId = null;

                // Validate and apply promotion code if provided
                if (!string.IsNullOrWhiteSpace(request.PromotionCode) && _promotionService != null)
                {
                    var validation = await _promotionService.ValidatePromotionAsync(new ValidatePromotionRequest
                    {
                        Code = request.PromotionCode,
                        UserId = userId,
                        HouseId = id,
                        Amount = originalTotalCost
                    });

                    if (!validation.IsValid)
                    {
                        // Release reservation on validation failure
                        if (reservationToken != null && _inventoryManager != null)
                        {
                            try
                            {
                                await _inventoryManager.ReleaseInventoryAsync(id, request.Quantity);
                                _logger.LogInformation(
                                    "Released reservation {ReservationToken} for house {HouseId} due to promotion validation failure",
                                    reservationToken, id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to release reservation {ReservationToken}", reservationToken);
                            }
                        }

                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                        {
                            Success = false,
                            Message = validation.Message ?? "Invalid promotion code",
                            Error = new ErrorResponse { Code = validation.ErrorCode ?? "PROMOTION_CODE_INVALID" }
                        });
                    }

                    // Apply discount
                    discountAmount = validation.DiscountAmount;
                    appliedPromotionId = validation.Promotion?.Id;
                    totalCost -= discountAmount;

                    // Ensure total cost is not negative
                    if (totalCost < 0)
                    {
                        totalCost = 0;
                    }

                    _logger.LogInformation(
                        "Promotion {PromotionCode} applied: Original cost {OriginalCost}, Discount {Discount}, Final cost {FinalCost}",
                        request.PromotionCode, originalTotalCost, discountAmount, totalCost);
                }

                // Process payment via Payment service (with discounted amount)
                var paymentResult = await _lotteryService.ProcessLotteryPaymentAsync(userId, id, request.Quantity, request.PaymentMethodId, totalCost);
                
                if (!paymentResult.Success)
                {
                    // Release reservation on payment failure
                    if (reservationToken != null && _inventoryManager != null)
                    {
                        try
                        {
                            await _inventoryManager.ReleaseInventoryAsync(id, request.Quantity);
                            _logger.LogInformation(
                                "Released reservation {ReservationToken} for house {HouseId} due to payment failure",
                                reservationToken, id);
                        }
                        catch (Exception releaseEx)
                        {
                            _logger.LogError(releaseEx, 
                                "Failed to release reservation {ReservationToken} for house {HouseId} after payment failure",
                                reservationToken, id);
                        }
                    }
                    
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                    {
                        Success = false,
                        Message = paymentResult.ErrorMessage ?? "Payment processing failed",
                        Error = new ErrorResponse
                        {
                            Code = "PAYMENT_FAILED",
                            Message = paymentResult.ErrorMessage ?? "Payment processing failed"
                        }
                    });
                }

                // Create tickets after successful payment
                var createTicketsRequest = new CreateTicketsFromPaymentRequest
                {
                    HouseId = id,
                    Quantity = request.Quantity,
                    PaymentId = paymentResult.TransactionId,
                    UserId = userId,
                    ReservationToken = reservationToken // Pass reservation token for confirmation
                };

                var ticketsResult = await _lotteryService.CreateTicketsFromPaymentAsync(createTicketsRequest);

                // Record promotion usage after successful payment
                if (appliedPromotionId.HasValue && !string.IsNullOrWhiteSpace(request.PromotionCode) && _promotionService != null)
                {
                    try
                    {
                        await _promotionService.ApplyPromotionAsync(new ApplyPromotionRequest
                        {
                            Code = request.PromotionCode,
                            UserId = userId,
                            HouseId = id,
                            Amount = originalTotalCost, // Original amount before discount
                            DiscountAmount = discountAmount,
                            TransactionId = paymentResult.TransactionId
                        });

                        _logger.LogInformation(
                            "Promotion {PromotionCode} usage recorded for user {UserId}, transaction {TransactionId}",
                            request.PromotionCode, userId, paymentResult.TransactionId);
                    }
                    catch (Exception promoEx)
                    {
                        // CRITICAL: Promotion discount was applied but usage not recorded
                        // This creates data inconsistency - discount given but not tracked
                        // Log as error with high severity for monitoring and manual reconciliation
                        _logger.LogError(promoEx,
                            "CRITICAL: Failed to record promotion usage after successful payment. " +
                            "Promotion {PromotionCode} discount {DiscountAmount} was applied to transaction {TransactionId} " +
                            "for user {UserId} but usage was not recorded. Manual reconciliation required.",
                            request.PromotionCode, discountAmount, paymentResult.TransactionId, userId);
                        
                        // TODO: Consider implementing compensation logic:
                        // 1. Create audit record for manual reconciliation
                        // 2. Or attempt to reverse the discount (complex - would require payment service integration)
                        // 3. Or mark transaction for review
                    }
                }

                // NEW: Confirm reservation after successful ticket creation
                // Note: Reservation will expire automatically, but we can release it immediately
                // since tickets are now created and inventory will be synced by InventorySyncService
                if (reservationToken != null && _inventoryManager != null)
                {
                    try
                    {
                        // Release reservation since tickets are now created
                        // The inventory count will be updated by InventorySyncService
                        await _inventoryManager.ReleaseInventoryAsync(id, request.Quantity);
                        _logger.LogInformation(
                            "Released reservation {ReservationToken} for house {HouseId} after successful ticket creation",
                            reservationToken, id);
                    }
                    catch (Exception releaseEx)
                    {
                        // Log but don't fail - tickets are already created
                        _logger.LogWarning(releaseEx, 
                            "Failed to release reservation {ReservationToken} for house {HouseId} after ticket creation (non-critical)",
                            reservationToken, id);
                    }
                }

                // Publish event for real-time updates
                await _eventPublisher.PublishAsync(new TicketPurchasedEvent
                {
                    UserId = userId,
                    HouseId = id,
                    TicketCount = ticketsResult.TicketsPurchased,
                    TicketNumbers = ticketsResult.TicketNumbers
                });

                // Invalidate house cache
                try
                {
                    await _cache.RemoveRecordAsync($"house_{id}");
                    await _cache.RemoveRecordAsync("houses_list");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate cache for house {HouseId}", id);
                }

                // Return response
                var response = new PurchaseTicketsResponse
                {
                    TicketsPurchased = ticketsResult.TicketsPurchased,
                    TotalCost = totalCost, // Final cost after discount
                    OriginalCost = originalTotalCost, // Cost before discount
                    DiscountAmount = discountAmount, // Discount applied
                    PromotionCode = request.PromotionCode, // Promotion code used
                    TicketNumbers = ticketsResult.TicketNumbers,
                    TransactionId = paymentResult.TransactionId.ToString(),
                    PaymentId = paymentResult.TransactionId
                };

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Tickets purchased successfully"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Verification check failed for ticket purchase");
                return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
                {
                    Success = false,
                    Message = ex.Message,
                    Error = new ErrorResponse { Code = "ID_VERIFICATION_REQUIRED", Message = ex.Message }
                });
            }
            catch (Exception ex)
            {
                // Release reservation on any exception
                if (reservationToken != null && _inventoryManager != null)
                {
                    try
                    {
                        await _inventoryManager.ReleaseInventoryAsync(id, request.Quantity);
                        _logger.LogInformation(
                            "Released reservation {ReservationToken} for house {HouseId} due to exception",
                            reservationToken, id);
                    }
                    catch (Exception releaseEx)
                    {
                        _logger.LogError(releaseEx, 
                            "Failed to release reservation {ReservationToken} for house {HouseId} after exception",
                            reservationToken, id);
                    }
                }
                
                _logger.LogError(ex, "Error purchasing tickets for house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<PurchaseTicketsResponse>
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

