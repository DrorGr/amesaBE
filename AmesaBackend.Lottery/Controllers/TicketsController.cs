using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ILotteryService _lotteryService;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(ILotteryService lotteryService, ILogger<TicketsController> logger)
        {
            _lotteryService = lotteryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<LotteryTicketDto>>>> GetUserTickets()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var tickets = await _lotteryService.GetUserTicketsAsync(userId);
                return Ok(new ApiResponse<List<LotteryTicketDto>> { Success = true, Data = tickets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user tickets");
                return StatusCode(500, new ApiResponse<List<LotteryTicketDto>> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<LotteryTicketDto>>> GetTicket(Guid id)
        {
            try
            {
                var ticket = await _lotteryService.GetTicketAsync(id);
                return Ok(new ApiResponse<LotteryTicketDto> { Success = true, Data = ticket });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<LotteryTicketDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ticket");
                return StatusCode(500, new ApiResponse<LotteryTicketDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        /// <summary>
        /// Get user's active lottery entries
        /// GET /api/v1/tickets/active
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<List<LotteryTicketDto>>>> GetActiveEntries()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<LotteryTicketDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var activeEntries = await _lotteryService.GetUserActiveEntriesAsync(userId);

                return Ok(new ApiResponse<List<LotteryTicketDto>>
                {
                    Success = true,
                    Data = activeEntries
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active entries");
                return StatusCode(500, new ApiResponse<List<LotteryTicketDto>>
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
        /// Get user's entry history with pagination and filters
        /// GET /api/v1/tickets/history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<PagedEntryHistoryResponse>>> GetEntryHistory([FromQuery] EntryFilters filters)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<PagedEntryHistoryResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var page = filters.Page > 0 ? filters.Page : 1;
                var limit = filters.Limit > 0 && filters.Limit <= 100 ? filters.Limit : 20;

                // Fixed: Use GetUserTicketsAsync to get ALL tickets, not just active ones
                var allTickets = await _lotteryService.GetUserTicketsAsync(userId);
                
                var filteredTickets = allTickets.AsQueryable();
                
                if (!string.IsNullOrEmpty(filters.Status))
                {
                    filteredTickets = filteredTickets.Where(t => t.Status == filters.Status);
                }
                
                if (filters.HouseId.HasValue)
                {
                    filteredTickets = filteredTickets.Where(t => t.HouseId == filters.HouseId.Value);
                }
                
                if (filters.StartDate.HasValue)
                {
                    filteredTickets = filteredTickets.Where(t => t.PurchaseDate >= filters.StartDate.Value);
                }
                
                if (filters.EndDate.HasValue)
                {
                    filteredTickets = filteredTickets.Where(t => t.PurchaseDate <= filters.EndDate.Value);
                }
                
                if (filters.IsWinner.HasValue)
                {
                    filteredTickets = filteredTickets.Where(t => t.IsWinner == filters.IsWinner.Value);
                }

                var total = filteredTickets.Count();
                var tickets = filteredTickets
                    .OrderByDescending(t => t.PurchaseDate)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToList();

                var response = new PagedEntryHistoryResponse
                {
                    Items = tickets, // Fixed: Changed from "Entries" to "Items" to match API contract
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / limit),
                    HasNext = page * limit < total,
                    HasPrevious = page > 1
                };

                return Ok(new ApiResponse<PagedEntryHistoryResponse>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entry history");
                return StatusCode(500, new ApiResponse<PagedEntryHistoryResponse>
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
        /// Get user's lottery analytics/statistics
        /// GET /api/v1/tickets/analytics
        /// </summary>
        [HttpGet("analytics")]
        public async Task<ActionResult<ApiResponse<UserLotteryStatsDto>>> GetAnalytics()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<UserLotteryStatsDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var stats = await _lotteryService.GetUserLotteryStatsAsync(userId);

                return Ok(new ApiResponse<UserLotteryStatsDto>
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics");
                return StatusCode(500, new ApiResponse<UserLotteryStatsDto>
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
        /// Quick entry - purchase tickets quickly
        /// POST /api/v1/tickets/quick-entry
        /// </summary>
        [HttpPost("quick-entry")]
        public async Task<ActionResult<ApiResponse<QuickEntryResponse>>> QuickEntry([FromBody] QuickEntryRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<QuickEntryResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // This would integrate with payment service
                // Fixed: Response structure matches API contract
                var response = new QuickEntryResponse
                {
                    TicketsPurchased = request.TicketCount, // Fixed: Changed to int (count) instead of List to match API contract
                    TotalCost = 0, // Fixed: Changed from "TotalAmount" to "TotalCost"
                    TicketNumbers = new List<string>(), // Fixed: Added TicketNumbers array
                    TransactionId = Guid.NewGuid().ToString(),
                    Message = "Quick entry functionality requires payment integration"
                };

                return Ok(new ApiResponse<QuickEntryResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Quick entry initiated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing quick entry");
                return StatusCode(500, new ApiResponse<QuickEntryResponse>
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
