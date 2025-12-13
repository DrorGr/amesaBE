using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Shared.Helpers;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Shared.Events;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ILotteryService _lotteryService;
        private readonly ILogger<TicketsController> _logger;
        private readonly LotteryDbContext _context;
        private readonly ICache? _cache;
        private readonly IEventPublisher? _eventPublisher;
        private readonly IPromotionService? _promotionService;

        public TicketsController(
            ILotteryService lotteryService, 
            ILogger<TicketsController> logger,
            LotteryDbContext context,
            ICache? cache = null,
            IEventPublisher? eventPublisher = null,
            IPromotionService? promotionService = null)
        {
            _lotteryService = lotteryService;
            _logger = logger;
            _context = context;
            _cache = cache;
            _eventPublisher = eventPublisher;
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryTicketDto>>>> GetUserTickets()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryTicketDto>> 
                    { 
                        Success = false, 
                        Message = "Authentication required" 
                    });
                }
                var tickets = await _lotteryService.GetUserTicketsAsync(userId);
                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryTicketDto>> { Success = true, Data = tickets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user tickets");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryTicketDto>> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<LotteryTicketDto>>> GetTicket(Guid id)
        {
            try
            {
                var ticket = await _lotteryService.GetTicketAsync(id);
                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryTicketDto> { Success = true, Data = ticket });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryTicketDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ticket");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryTicketDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        /// <summary>
        /// Get user's active lottery entries
        /// GET /api/v1/tickets/active
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryTicketDto>>>> GetActiveEntries()
        {
            // #region agent log
            _logger.LogInformation("[DEBUG] TicketsController.GetActiveEntries:entry");
            // #endregion
            try
            {
                // #region agent log
                _logger.LogInformation("[DEBUG] TicketsController.GetActiveEntries:before-user-claim");
                // #endregion
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // #region agent log
                _logger.LogInformation("[DEBUG] TicketsController.GetActiveEntries:after-user-claim userIdClaim={UserIdClaim}", userIdClaim);
                // #endregion
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    // #region agent log
                    _logger.LogWarning("[DEBUG] TicketsController.GetActiveEntries:unauthorized");
                    // #endregion
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryTicketDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // #region agent log
                _logger.LogInformation("[DEBUG] TicketsController.GetActiveEntries:before-service-call userId={UserId}", userId);
                // #endregion
                var activeEntries = await _lotteryService.GetUserActiveEntriesAsync(userId);
                // #region agent log
                _logger.LogInformation("[DEBUG] TicketsController.GetActiveEntries:after-service-call userId={UserId} entriesCount={Count}", userId, activeEntries.Count);
                // #endregion

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryTicketDto>>
                {
                    Success = true,
                    Data = activeEntries
                });
            }
            catch (Exception ex)
            {
                // #region agent log
                _logger.LogError(ex, "[DEBUG] TicketsController.GetActiveEntries:exception exceptionType={Type} message={Message} stackTrace={StackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0)));
                // #endregion
                _logger.LogError(ex, "Error retrieving active entries");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryTicketDto>>
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
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<PagedEntryHistoryResponse>>> GetEntryHistory([FromQuery] EntryFilters filters)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedEntryHistoryResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var page = filters.Page > 0 ? filters.Page : 1;
                var limit = filters.Limit > 0 && filters.Limit <= 100 ? filters.Limit : 20;

                // Fixed: Filter at database level instead of loading all tickets into memory
                // This significantly improves performance for users with many tickets
                var query = _context.LotteryTickets
                    .Include(t => t.House)
                    .AsNoTracking() // Read-only query - no need for change tracking
                    .Where(t => t.UserId == userId);
                
                // Apply filters at database level
                if (!string.IsNullOrEmpty(filters.Status))
                {
                    query = query.Where(t => t.Status == filters.Status);
                }
                
                if (filters.HouseId.HasValue)
                {
                    query = query.Where(t => t.HouseId == filters.HouseId.Value);
                }
                
                if (filters.StartDate.HasValue)
                {
                    query = query.Where(t => t.PurchaseDate >= filters.StartDate.Value);
                }
                
                if (filters.EndDate.HasValue)
                {
                    query = query.Where(t => t.PurchaseDate <= filters.EndDate.Value);
                }
                
                if (filters.IsWinner.HasValue)
                {
                    query = query.Where(t => t.IsWinner == filters.IsWinner.Value);
                }

                // Get total count before pagination
                var total = await query.CountAsync();
                
                // Apply pagination and ordering at database level
                var tickets = await query
                    .OrderByDescending(t => t.PurchaseDate)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();
                
                // Map to DTOs
                var ticketDtos = tickets.Select(t => new LotteryTicketDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    HouseId = t.HouseId,
                    HouseTitle = t.House?.Title ?? "",
                    PurchasePrice = t.PurchasePrice,
                    Status = t.Status,
                    PurchaseDate = t.PurchaseDate,
                    IsWinner = t.IsWinner,
                    CreatedAt = t.CreatedAt
                }).ToList();

                var response = new PagedEntryHistoryResponse
                {
                    Items = ticketDtos, // Fixed: Changed from "Entries" to "Items" to match API contract
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / limit),
                    HasNext = page * limit < total,
                    HasPrevious = page > 1
                };

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedEntryHistoryResponse>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entry history");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<PagedEntryHistoryResponse>
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
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<UserLotteryStatsDto>>> GetAnalytics()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<UserLotteryStatsDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var stats = await _lotteryService.GetUserLotteryStatsAsync(userId);

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<UserLotteryStatsDto>
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<UserLotteryStatsDto>
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
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>>> QuickEntry([FromBody] QuickEntryRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Check ID verification requirement
                await _lotteryService.CheckVerificationRequirementAsync(userId);

                // Get house to calculate total cost (read-only - use AsNoTracking for performance)
                var house = await _context.Houses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == request.HouseId);
                if (house == null)
                {
                    return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>
                    {
                        Success = false,
                        Message = "House not found"
                    });
                }

                var originalTotalCost = house.TicketPrice * request.TicketCount;
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
                        HouseId = request.HouseId,
                        Amount = originalTotalCost
                    });

                    if (!validation.IsValid)
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>
                        {
                            Success = false,
                            Message = validation.Message ?? "Invalid promotion code",
                            Error = new ErrorResponse 
                            { 
                                Code = validation.ErrorCode ?? "PROMOTION_CODE_INVALID",
                                Message = validation.Message ?? "The provided promotion code is not valid or applicable."
                            }
                        });
                    }

                    discountAmount = validation.DiscountAmount;
                    appliedPromotionId = validation.Promotion?.Id;
                    totalCost -= discountAmount;

                    // Ensure total cost is not negative
                    if (totalCost < 0)
                    {
                        totalCost = 0;
                    }

                    _logger.LogInformation(
                        "Promotion {PromotionCode} applied to quick entry: Original cost {OriginalCost}, Discount {Discount}, Final cost {FinalCost}",
                        request.PromotionCode, originalTotalCost, discountAmount, totalCost);
                }

                // Check participant cap
                // NOTE: CanUserEnterLotteryAsync uses transaction safety (Serializable isolation) when useTransaction=true (default)
                // This prevents race conditions where multiple concurrent requests could bypass the participant cap
                // Ticket creation in CreateTicketsFromPaymentAsync also uses transaction safety with cap check
                var canEnter = await _lotteryService.CanUserEnterLotteryAsync(userId, request.HouseId);
                if (!canEnter)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>
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

                // Process payment via Payment service
                var paymentResult = await _lotteryService.ProcessLotteryPaymentAsync(userId, request.HouseId, request.TicketCount, request.PaymentMethodId, totalCost);
                
                if (!paymentResult.Success)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>
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
                // CreateTicketsFromPaymentAsync includes transaction safety and cap check
                var createTicketsRequest = new CreateTicketsFromPaymentRequest
                {
                    HouseId = request.HouseId,
                    Quantity = request.TicketCount,
                    PaymentId = paymentResult.TransactionId,
                    UserId = userId
                };

                var ticketsResult = await _lotteryService.CreateTicketsFromPaymentAsync(createTicketsRequest);

                // Record promotion usage if promotion was applied
                if (appliedPromotionId.HasValue && !string.IsNullOrWhiteSpace(request.PromotionCode) && _promotionService != null)
                {
                    try
                    {
                        await _promotionService.ApplyPromotionAsync(new ApplyPromotionRequest
                        {
                            Code = request.PromotionCode,
                            UserId = userId,
                            HouseId = request.HouseId,
                            Amount = originalTotalCost, // Original amount before discount
                            DiscountAmount = discountAmount,
                            TransactionId = paymentResult.TransactionId
                        });
                        _logger.LogInformation("Promotion {PromotionCode} usage recorded for user {UserId} with transaction {TransactionId}",
                            request.PromotionCode, userId, paymentResult.TransactionId);
                    }
                    catch (Exception ex)
                    {
                        // CRITICAL: Promotion discount was applied but usage not recorded
                        // This creates data inconsistency - discount given but not tracked
                        // Log as error with high severity for monitoring and manual reconciliation
                        _logger.LogError(ex,
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

                // Publish event for real-time updates
                if (_eventPublisher != null)
                {
                    await _eventPublisher.PublishAsync(new TicketPurchasedEvent
                    {
                        UserId = userId,
                        HouseId = request.HouseId,
                        TicketCount = ticketsResult.TicketsPurchased,
                        TicketNumbers = ticketsResult.TicketNumbers
                    });
                }

                // Invalidate house cache
                if (_cache != null)
                {
                    try
                    {
                        await _cache.RemoveRecordAsync($"house_{request.HouseId}");
                        await _cache.RemoveRecordAsync("houses_list");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to invalidate cache for house {HouseId}", request.HouseId);
                    }
                }

                // Return response with real values
                var response = new QuickEntryResponse
                {
                    TicketsPurchased = ticketsResult.TicketsPurchased,
                    OriginalCost = originalTotalCost,
                    DiscountAmount = discountAmount,
                    TotalCost = totalCost,
                    PromotionCode = request.PromotionCode,
                    TicketNumbers = ticketsResult.TicketNumbers,
                    TransactionId = paymentResult.TransactionId.ToString(),
                    Message = "Tickets purchased successfully"
                };

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Quick entry completed successfully"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Verification check failed for quick entry");
                return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>
                {
                    Success = false,
                    Message = ex.Message,
                    Error = new ErrorResponse { Code = "ID_VERIFICATION_REQUIRED", Message = ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing quick entry");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<QuickEntryResponse>
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
