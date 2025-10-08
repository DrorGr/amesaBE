using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Data;
using AmesaBackend.DTOs;
using AmesaBackend.Models;
using System.Security.Claims;

namespace AmesaBackend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HousesController : ControllerBase
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<HousesController> _logger;

        public HousesController(AmesaDbContext context, ILogger<HousesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all houses with pagination and filtering
        /// </summary>
        [HttpGet]
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
                var query = _context.Houses
                    .Include(h => h.Images)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<LotteryStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(h => h.Status == statusEnum);
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

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var houses = await query
                    .OrderByDescending(h => h.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                // Get ticket counts for each house
                var houseIds = houses.Select(h => h.Id).ToList();
                var ticketCounts = await _context.LotteryTickets
                    .Where(t => houseIds.Contains(t.HouseId) && t.Status == TicketStatus.Active)
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
                        Status = house.Status.ToString(),
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
                            MediaType = i.MediaType.ToString(),
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

                return Ok(new ApiResponse<PagedResponse<HouseDto>>
                {
                    Success = true,
                    Data = pagedResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving houses");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while retrieving houses"
                    }
                });
            }
        }

        /// <summary>
        /// Get house details by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<HouseDto>>> GetHouse(Guid id)
        {
            try
            {
                var house = await _context.Houses
                    .Include(h => h.Images)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (house == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "NOT_FOUND",
                            Message = "House not found"
                        }
                    });
                }

                var ticketsSold = await _context.LotteryTickets
                    .CountAsync(t => t.HouseId == id && t.Status == TicketStatus.Active);

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
                    Status = house.Status.ToString(),
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
                        MediaType = i.MediaType.ToString(),
                        FileSize = i.FileSize,
                        Width = i.Width,
                        Height = i.Height
                    }).ToList(),
                    CreatedAt = house.CreatedAt
                };

                return Ok(new ApiResponse<HouseDto>
                {
                    Success = true,
                    Data = houseDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving house {HouseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while retrieving house details"
                    }
                });
            }
        }

        /// <summary>
        /// Get available tickets for a house
        /// </summary>
        [HttpGet("{id}/tickets")]
        public async Task<ActionResult<ApiResponse<object>>> GetAvailableTickets(Guid id)
        {
            try
            {
                var house = await _context.Houses.FindAsync(id);
                if (house == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "NOT_FOUND",
                            Message = "House not found"
                        }
                    });
                }

                var ticketsSold = await _context.LotteryTickets
                    .CountAsync(t => t.HouseId == id && t.Status == TicketStatus.Active);

                var availableTickets = house.TotalTickets - ticketsSold;

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        TotalTickets = house.TotalTickets,
                        TicketsSold = ticketsSold,
                        AvailableTickets = availableTickets,
                        TicketPrice = house.TicketPrice,
                        CanPurchase = availableTickets > 0 && house.Status == LotteryStatus.Active
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available tickets for house {HouseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while retrieving ticket information"
                    }
                });
            }
        }

        /// <summary>
        /// Purchase lottery tickets (requires authentication)
        /// </summary>
        [HttpPost("{id}/tickets/purchase")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> PurchaseTickets(Guid id, [FromBody] PurchaseTicketRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");

                var house = await _context.Houses.FindAsync(id);
                if (house == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "NOT_FOUND",
                            Message = "House not found"
                        }
                    });
                }

                if (house.Status != LotteryStatus.Active)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "INVALID_OPERATION",
                            Message = "Lottery is not active for this house"
                        }
                    });
                }

                var ticketsSold = await _context.LotteryTickets
                    .CountAsync(t => t.HouseId == id && t.Status == TicketStatus.Active);

                if (ticketsSold + request.Quantity > house.TotalTickets)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "INSUFFICIENT_TICKETS",
                            Message = "Not enough tickets available"
                        }
                    });
                }

                // TODO: Implement payment processing
                // For now, we'll create the tickets without payment
                var tickets = new List<LotteryTicket>();
                for (int i = 0; i < request.Quantity; i++)
                {
                    var ticket = new LotteryTicket
                    {
                        TicketNumber = GenerateTicketNumber(),
                        HouseId = id,
                        UserId = userId,
                        PurchasePrice = house.TicketPrice,
                        Status = TicketStatus.Active,
                        PurchaseDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    tickets.Add(ticket);
                }

                _context.LotteryTickets.AddRange(tickets);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} purchased {Quantity} tickets for house {HouseId}", userId, request.Quantity, id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        TicketsPurchased = request.Quantity,
                        TotalCost = house.TicketPrice * request.Quantity,
                        TicketNumbers = tickets.Select(t => t.TicketNumber).ToList()
                    },
                    Message = "Tickets purchased successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purchasing tickets for house {HouseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while purchasing tickets"
                    }
                });
            }
        }


        private string GenerateTicketNumber()
        {
            return "TK" + DateTime.UtcNow.Ticks.ToString()[^8..];
        }
    }

    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}
