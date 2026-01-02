using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AmesaBackend.LotteryResults.Data;
using AmesaBackend.LotteryResults.DTOs;
using AmesaBackend.LotteryResults.Models;
using AmesaBackend.LotteryResults.Services;
using AmesaBackend.LotteryResults.Services.Interfaces;
using AmesaBackend.Shared.Events;

namespace AmesaBackend.LotteryResults.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LotteryResultsController : ControllerBase
    {
        private readonly LotteryResultsDbContext _context;
        private readonly ILogger<LotteryResultsController> _logger;
        private readonly IQRCodeService _qrCodeService;
        private readonly IEventPublisher _eventPublisher;

        public LotteryResultsController(
            LotteryResultsDbContext context,
            ILogger<LotteryResultsController> logger,
            IQRCodeService qrCodeService,
            IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _qrCodeService = qrCodeService;
            _eventPublisher = eventPublisher;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<LotteryResultsPageDto>>> GetLotteryResults([FromQuery] LotteryResultsFilterDto filter)
        {
            try
            {
                var query = _context.LotteryResults.AsQueryable();

                if (filter.FromDate.HasValue)
                    query = query.Where(lr => lr.ResultDate >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(lr => lr.ResultDate <= filter.ToDate.Value);

                if (filter.PrizePosition.HasValue)
                    query = query.Where(lr => lr.PrizePosition == filter.PrizePosition.Value);

                if (!string.IsNullOrEmpty(filter.PrizeType))
                    query = query.Where(lr => lr.PrizeType == filter.PrizeType);

                if (filter.IsClaimed.HasValue)
                    query = query.Where(lr => lr.IsClaimed == filter.IsClaimed.Value);

                query = filter.SortBy?.ToLower() switch
                {
                    "resultdate" => filter.SortDirection?.ToLower() == "asc" 
                        ? query.OrderBy(lr => lr.ResultDate)
                        : query.OrderByDescending(lr => lr.ResultDate),
                    "prizevalue" => filter.SortDirection?.ToLower() == "asc"
                        ? query.OrderBy(lr => lr.PrizeValue)
                        : query.OrderByDescending(lr => lr.PrizeValue),
                    _ => query.OrderByDescending(lr => lr.ResultDate)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

                var results = await query
                    .AsNoTracking()
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                // Get house and winner info from other services via HTTP
                var resultDtos = new List<LotteryResultDto>();
                foreach (var result in results)
                {
                    // In a real implementation, you'd fetch house and winner info from Lottery and Auth services
                    // Note: WinnerUserId is excluded from public responses for privacy
                    resultDtos.Add(new LotteryResultDto
                    {
                        Id = result.Id,
                        LotteryId = result.LotteryId,
                        DrawId = result.DrawId,
                        WinnerTicketNumber = result.WinnerTicketNumber,
                        // WinnerUserId removed from public response for privacy
                        PrizePosition = result.PrizePosition,
                        PrizeType = result.PrizeType,
                        PrizeValue = result.PrizeValue,
                        PrizeDescription = result.PrizeDescription,
                        QRCodeData = result.QRCodeData,
                        QRCodeImageUrl = result.QRCodeImageUrl,
                        IsVerified = result.IsVerified,
                        IsClaimed = result.IsClaimed,
                        ClaimedAt = result.ClaimedAt,
                        ResultDate = result.ResultDate
                    });
                }

                var pageDto = new LotteryResultsPageDto
                {
                    Results = resultDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = filter.PageNumber < totalPages,
                    HasPreviousPage = filter.PageNumber > 1
                };

                return Ok(new ApiResponse<LotteryResultsPageDto>
                {
                    Success = true,
                    Data = pageDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lottery results");
                return StatusCode(500, new ApiResponse<LotteryResultsPageDto>
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
        public async Task<ActionResult<ApiResponse<LotteryResultDto>>> GetLotteryResult(Guid id)
        {
            try
            {
                var result = await _context.LotteryResults
                    .AsNoTracking()
                    .Include(lr => lr.History)
                    .FirstOrDefaultAsync(lr => lr.Id == id);

                if (result == null)
                {
                    return NotFound(new ApiResponse<LotteryResultDto>
                    {
                        Success = false,
                        Message = "Lottery result not found"
                    });
                }

                // Note: WinnerUserId excluded from public response for privacy
                var resultDto = new LotteryResultDto
                {
                    Id = result.Id,
                    LotteryId = result.LotteryId,
                    DrawId = result.DrawId,
                    WinnerTicketNumber = result.WinnerTicketNumber,
                    // WinnerUserId removed from public response for privacy
                    PrizePosition = result.PrizePosition,
                    PrizeType = result.PrizeType,
                    PrizeValue = result.PrizeValue,
                    PrizeDescription = result.PrizeDescription,
                    QRCodeData = result.QRCodeData,
                    QRCodeImageUrl = result.QRCodeImageUrl,
                    IsVerified = result.IsVerified,
                    IsClaimed = result.IsClaimed,
                    ClaimedAt = result.ClaimedAt,
                    ResultDate = result.ResultDate
                };

                return Ok(new ApiResponse<LotteryResultDto>
                {
                    Success = true,
                    Data = resultDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lottery result {Id}", id);
                return StatusCode(500, new ApiResponse<LotteryResultDto>
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

        [HttpPost("validate-qr")]
        public async Task<ActionResult<ApiResponse<QRCodeValidationDto>>> ValidateQRCode([FromBody] ValidateQRCodeRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.QRCodeData))
                {
                    return BadRequest(new ApiResponse<QRCodeValidationDto>
                    {
                        Success = false,
                        Message = "QR code data is required"
                    });
                }

                var validationResult = await _qrCodeService.DecodeQRCodeAsync(request.QRCodeData);
                
                if (!validationResult.IsValid)
                {
                    return Ok(new ApiResponse<QRCodeValidationDto>
                    {
                        Success = true,
                        Data = new QRCodeValidationDto
                        {
                            IsValid = false,
                            IsWinner = false,
                            Message = validationResult.ErrorMessage ?? "Invalid QR code"
                        }
                    });
                }

                var lotteryResult = await _context.LotteryResults
                    .FirstOrDefaultAsync(lr => lr.Id == validationResult.LotteryResultId);

                if (lotteryResult == null)
                {
                    return Ok(new ApiResponse<QRCodeValidationDto>
                    {
                        Success = true,
                        Data = new QRCodeValidationDto
                        {
                            IsValid = false,
                            IsWinner = false,
                            Message = "Lottery result not found"
                        }
                    });
                }

                var validationDto = new QRCodeValidationDto
                {
                    IsValid = true,
                    IsWinner = true,
                    PrizePosition = lotteryResult.PrizePosition,
                    PrizeType = lotteryResult.PrizeType,
                    PrizeValue = lotteryResult.PrizeValue,
                    PrizeDescription = lotteryResult.PrizeDescription,
                    IsClaimed = lotteryResult.IsClaimed,
                    Message = $"Congratulations! You won {lotteryResult.PrizeDescription}"
                };

                return Ok(new ApiResponse<QRCodeValidationDto>
                {
                    Success = true,
                    Data = validationDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code");
                return StatusCode(500, new ApiResponse<QRCodeValidationDto>
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

        [HttpPost("claim")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<LotteryResultDto>>> ClaimPrize([FromBody] ClaimPrizeRequest request)
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserId))
                {
                    return Unauthorized(new ApiResponse<LotteryResultDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var lotteryResult = await _context.LotteryResults
                    .FirstOrDefaultAsync(lr => lr.Id == request.ResultId);

                if (lotteryResult == null)
                {
                    return NotFound(new ApiResponse<LotteryResultDto>
                    {
                        Success = false,
                        Message = "Lottery result not found"
                    });
                }

                // Verify user is the winner
                if (lotteryResult.WinnerUserId != currentUserId)
                {
                    return Unauthorized(new ApiResponse<LotteryResultDto>
                    {
                        Success = false,
                        Message = "You are not authorized to claim this prize. Only the winner can claim their prize."
                    });
                }

                if (lotteryResult.IsClaimed)
                {
                    return BadRequest(new ApiResponse<LotteryResultDto>
                    {
                        Success = false,
                        Message = "Prize has already been claimed"
                    });
                }

                lotteryResult.IsClaimed = true;
                lotteryResult.ClaimedAt = DateTime.UtcNow;
                lotteryResult.ClaimNotes = request.ClaimNotes;
                lotteryResult.UpdatedAt = DateTime.UtcNow;

                var historyEntry = new LotteryResultHistory
                {
                    Id = Guid.NewGuid(),
                    LotteryResultId = lotteryResult.Id,
                    Action = "Claimed",
                    Details = $"Prize claimed. Notes: {request.ClaimNotes ?? "None"}",
                    PerformedBy = "System",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                _context.LotteryResultHistory.Add(historyEntry);
                await _context.SaveChangesAsync();

                await _eventPublisher.PublishAsync(new PrizeClaimedEvent
                {
                    ResultId = lotteryResult.Id,
                    UserId = lotteryResult.WinnerUserId,
                    ClaimedAt = lotteryResult.ClaimedAt ?? DateTime.UtcNow
                });

                var resultDto = new LotteryResultDto
                {
                    Id = lotteryResult.Id,
                    LotteryId = lotteryResult.LotteryId,
                    DrawId = lotteryResult.DrawId,
                    WinnerTicketNumber = lotteryResult.WinnerTicketNumber,
                    WinnerUserId = lotteryResult.WinnerUserId,
                    PrizePosition = lotteryResult.PrizePosition,
                    PrizeType = lotteryResult.PrizeType,
                    PrizeValue = lotteryResult.PrizeValue,
                    PrizeDescription = lotteryResult.PrizeDescription,
                    IsVerified = lotteryResult.IsVerified,
                    IsClaimed = lotteryResult.IsClaimed,
                    ClaimedAt = lotteryResult.ClaimedAt,
                    ResultDate = lotteryResult.ResultDate
                };

                return Ok(new ApiResponse<LotteryResultDto>
                {
                    Success = true,
                    Data = resultDto,
                    Message = "Prize claimed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming prize for result {ResultId}", request.ResultId);
                return StatusCode(500, new ApiResponse<LotteryResultDto>
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

        [HttpPost("delivery")]
        public async Task<ActionResult<ApiResponse<PrizeDeliveryDto>>> CreatePrizeDelivery([FromBody] CreatePrizeDeliveryRequest request)
        {
            try
            {
                var lotteryResult = await _context.LotteryResults
                    .FirstOrDefaultAsync(lr => lr.Id == request.LotteryResultId);

                if (lotteryResult == null)
                {
                    return NotFound(new ApiResponse<PrizeDeliveryDto>
                    {
                        Success = false,
                        Message = "Lottery result not found"
                    });
                }

                if (lotteryResult.PrizePosition == 1)
                {
                    return BadRequest(new ApiResponse<PrizeDeliveryDto>
                    {
                        Success = false,
                        Message = "First place winners do not require delivery (house prize)"
                    });
                }

                var delivery = new PrizeDelivery
                {
                    Id = Guid.NewGuid(),
                    LotteryResultId = request.LotteryResultId,
                    WinnerUserId = lotteryResult.WinnerUserId,
                    RecipientName = request.RecipientName,
                    AddressLine1 = request.AddressLine1,
                    AddressLine2 = request.AddressLine2,
                    City = request.City,
                    State = request.State,
                    PostalCode = request.PostalCode,
                    Country = request.Country,
                    Phone = request.Phone,
                    Email = request.Email,
                    DeliveryMethod = request.DeliveryMethod,
                    DeliveryStatus = "Pending",
                    DeliveryNotes = request.DeliveryNotes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PrizeDeliveries.Add(delivery);
                await _context.SaveChangesAsync();

                var deliveryDto = new PrizeDeliveryDto
                {
                    Id = delivery.Id,
                    LotteryResultId = delivery.LotteryResultId,
                    RecipientName = delivery.RecipientName,
                    AddressLine1 = delivery.AddressLine1,
                    AddressLine2 = delivery.AddressLine2,
                    City = delivery.City,
                    State = delivery.State,
                    PostalCode = delivery.PostalCode,
                    Country = delivery.Country,
                    Phone = delivery.Phone,
                    Email = delivery.Email,
                    DeliveryMethod = delivery.DeliveryMethod,
                    DeliveryStatus = delivery.DeliveryStatus,
                    EstimatedDeliveryDate = delivery.EstimatedDeliveryDate,
                    ShippingCost = delivery.ShippingCost,
                    DeliveryNotes = delivery.DeliveryNotes
                };

                return Ok(new ApiResponse<PrizeDeliveryDto>
                {
                    Success = true,
                    Data = deliveryDto,
                    Message = "Prize delivery request created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prize delivery");
                return StatusCode(500, new ApiResponse<PrizeDeliveryDto>
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

