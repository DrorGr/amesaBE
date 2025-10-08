using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Data;
using AmesaBackend.DTOs;
using AmesaBackend.Models;
using AmesaBackend.Services;

namespace AmesaBackend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LotteryResultsController : ControllerBase
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<LotteryResultsController> _logger;
        private readonly IQRCodeService _qrCodeService;

        public LotteryResultsController(
            AmesaDbContext context,
            ILogger<LotteryResultsController> logger,
            IQRCodeService qrCodeService)
        {
            _context = context;
            _logger = logger;
            _qrCodeService = qrCodeService;
        }

        /// <summary>
        /// Get filtered lottery results with pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<LotteryResultsPageDto>>> GetLotteryResults([FromQuery] LotteryResultsFilterDto filter)
        {
            try
            {
                var query = _context.LotteryResults
                    .Include(lr => lr.House)
                    .Include(lr => lr.Winner)
                    .AsQueryable();

                // Apply filters
                if (filter.FromDate.HasValue)
                    query = query.Where(lr => lr.ResultDate >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(lr => lr.ResultDate <= filter.ToDate.Value);

                if (!string.IsNullOrEmpty(filter.Address))
                    query = query.Where(lr => lr.House.Address.Contains(filter.Address));

                if (!string.IsNullOrEmpty(filter.City))
                    query = query.Where(lr => lr.House.Location.Contains(filter.City));

                if (filter.PrizePosition.HasValue)
                    query = query.Where(lr => lr.PrizePosition == filter.PrizePosition.Value);

                if (!string.IsNullOrEmpty(filter.PrizeType))
                    query = query.Where(lr => lr.PrizeType == filter.PrizeType);

                if (filter.IsClaimed.HasValue)
                    query = query.Where(lr => lr.IsClaimed == filter.IsClaimed.Value);

                // Apply sorting
                query = filter.SortBy?.ToLower() switch
                {
                    "resultdate" => filter.SortDirection?.ToLower() == "asc" 
                        ? query.OrderBy(lr => lr.ResultDate)
                        : query.OrderByDescending(lr => lr.ResultDate),
                    "prizevalue" => filter.SortDirection?.ToLower() == "asc"
                        ? query.OrderBy(lr => lr.PrizeValue)
                        : query.OrderByDescending(lr => lr.PrizeValue),
                    "prizeposition" => filter.SortDirection?.ToLower() == "asc"
                        ? query.OrderBy(lr => lr.PrizePosition)
                        : query.OrderByDescending(lr => lr.PrizePosition),
                    _ => query.OrderByDescending(lr => lr.ResultDate)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

                var results = await query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(lr => new LotteryResultDto
                    {
                        Id = lr.Id,
                        LotteryId = lr.LotteryId,
                        DrawId = lr.DrawId,
                        WinnerTicketNumber = lr.WinnerTicketNumber,
                        WinnerUserId = lr.WinnerUserId,
                        PrizePosition = lr.PrizePosition,
                        PrizeType = lr.PrizeType,
                        PrizeValue = lr.PrizeValue,
                        PrizeDescription = lr.PrizeDescription,
                        QRCodeData = lr.QRCodeData,
                        QRCodeImageUrl = lr.QRCodeImageUrl,
                        IsVerified = lr.IsVerified,
                        IsClaimed = lr.IsClaimed,
                        ClaimedAt = lr.ClaimedAt,
                        ResultDate = lr.ResultDate,
                        HouseTitle = lr.House.Title,
                        HouseAddress = lr.House.Address,
                        WinnerName = $"{lr.Winner.FirstName} {lr.Winner.LastName}",
                        WinnerEmail = lr.Winner.Email
                    })
                    .ToListAsync();

                var pageDto = new LotteryResultsPageDto
                {
                    Results = results,
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
                    Data = pageDto,
                    Message = "Lottery results retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lottery results");
                return StatusCode(500, new ApiResponse<LotteryResultsPageDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving lottery results",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get specific lottery result by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<LotteryResultDto>>> GetLotteryResult(Guid id)
        {
            try
            {
                var result = await _context.LotteryResults
                    .Include(lr => lr.House)
                    .Include(lr => lr.Winner)
                    .Include(lr => lr.Draw)
                    .FirstOrDefaultAsync(lr => lr.Id == id);

                if (result == null)
                {
                    return NotFound(new ApiResponse<LotteryResultDto>
                    {
                        Success = false,
                        Message = "Lottery result not found",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var resultDto = new LotteryResultDto
                {
                    Id = result.Id,
                    LotteryId = result.LotteryId,
                    DrawId = result.DrawId,
                    WinnerTicketNumber = result.WinnerTicketNumber,
                    WinnerUserId = result.WinnerUserId,
                    PrizePosition = result.PrizePosition,
                    PrizeType = result.PrizeType,
                    PrizeValue = result.PrizeValue,
                    PrizeDescription = result.PrizeDescription,
                    QRCodeData = result.QRCodeData,
                    QRCodeImageUrl = result.QRCodeImageUrl,
                    IsVerified = result.IsVerified,
                    IsClaimed = result.IsClaimed,
                    ClaimedAt = result.ClaimedAt,
                    ResultDate = result.ResultDate,
                    HouseTitle = result.House.Title,
                    HouseAddress = result.House.Address,
                    WinnerName = $"{result.Winner.FirstName} {result.Winner.LastName}",
                    WinnerEmail = result.Winner.Email
                };

                return Ok(new ApiResponse<LotteryResultDto>
                {
                    Success = true,
                    Data = resultDto,
                    Message = "Lottery result retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lottery result {Id}", id);
                return StatusCode(500, new ApiResponse<LotteryResultDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving lottery result",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Validate QR code and get winner information
        /// </summary>
        [HttpPost("validate-qr")]
        public async Task<ActionResult<ApiResponse<QRCodeValidationDto>>> ValidateQRCode([FromBody] string qrCodeData)
        {
            try
            {
                var validationResult = await _qrCodeService.DecodeQRCodeAsync(qrCodeData);
                
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
                        },
                        Timestamp = DateTime.UtcNow
                    });
                }

                var lotteryResult = await _context.LotteryResults
                    .Include(lr => lr.House)
                    .Include(lr => lr.Winner)
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
                        },
                        Timestamp = DateTime.UtcNow
                    });
                }

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
                    ResultDate = lotteryResult.ResultDate,
                    HouseTitle = lotteryResult.House.Title,
                    HouseAddress = lotteryResult.House.Address,
                    WinnerName = $"{lotteryResult.Winner.FirstName} {lotteryResult.Winner.LastName}",
                    WinnerEmail = lotteryResult.Winner.Email
                };

                var validationDto = new QRCodeValidationDto
                {
                    IsValid = true,
                    IsWinner = true,
                    PrizePosition = lotteryResult.PrizePosition,
                    PrizeType = lotteryResult.PrizeType,
                    PrizeValue = lotteryResult.PrizeValue,
                    PrizeDescription = lotteryResult.PrizeDescription,
                    IsClaimed = lotteryResult.IsClaimed,
                    Message = $"Congratulations! You won {lotteryResult.PrizeDescription}",
                    Result = resultDto
                };

                return Ok(new ApiResponse<QRCodeValidationDto>
                {
                    Success = true,
                    Data = validationDto,
                    Message = "QR code validated successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code");
                return StatusCode(500, new ApiResponse<QRCodeValidationDto>
                {
                    Success = false,
                    Message = "An error occurred while validating QR code",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Claim a prize
        /// </summary>
        [HttpPost("claim")]
        public async Task<ActionResult<ApiResponse<LotteryResultDto>>> ClaimPrize([FromBody] ClaimPrizeRequest request)
        {
            try
            {
                var lotteryResult = await _context.LotteryResults
                    .Include(lr => lr.House)
                    .Include(lr => lr.Winner)
                    .FirstOrDefaultAsync(lr => lr.Id == request.ResultId);

                if (lotteryResult == null)
                {
                    return NotFound(new ApiResponse<LotteryResultDto>
                    {
                        Success = false,
                        Message = "Lottery result not found",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (lotteryResult.IsClaimed)
                {
                    return BadRequest(new ApiResponse<LotteryResultDto>
                    {
                        Success = false,
                        Message = "Prize has already been claimed",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Update lottery result
                lotteryResult.IsClaimed = true;
                lotteryResult.ClaimedAt = DateTime.UtcNow;
                lotteryResult.ClaimNotes = request.ClaimNotes;
                lotteryResult.UpdatedAt = DateTime.UtcNow;

                // Add history entry
                var historyEntry = new LotteryResultHistory
                {
                    Id = Guid.NewGuid(),
                    LotteryResultId = lotteryResult.Id,
                    Action = "Claimed",
                    Details = $"Prize claimed by winner. Notes: {request.ClaimNotes ?? "None"}",
                    PerformedBy = lotteryResult.Winner.Email,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                _context.LotteryResultHistory.Add(historyEntry);
                await _context.SaveChangesAsync();

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
                    ResultDate = lotteryResult.ResultDate,
                    HouseTitle = lotteryResult.House.Title,
                    HouseAddress = lotteryResult.House.Address,
                    WinnerName = $"{lotteryResult.Winner.FirstName} {lotteryResult.Winner.LastName}",
                    WinnerEmail = lotteryResult.Winner.Email
                };

                return Ok(new ApiResponse<LotteryResultDto>
                {
                    Success = true,
                    Data = resultDto,
                    Message = "Prize claimed successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming prize for result {ResultId}", request.ResultId);
                return StatusCode(500, new ApiResponse<LotteryResultDto>
                {
                    Success = false,
                    Message = "An error occurred while claiming prize",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Create prize delivery request for 2nd and 3rd place winners
        /// </summary>
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
                        Message = "Lottery result not found",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (lotteryResult.PrizePosition == 1)
                {
                    return BadRequest(new ApiResponse<PrizeDeliveryDto>
                    {
                        Success = false,
                        Message = "First place winners do not require delivery (house prize)",
                        Timestamp = DateTime.UtcNow
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
                    Message = "Prize delivery request created successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prize delivery");
                return StatusCode(500, new ApiResponse<PrizeDeliveryDto>
                {
                    Success = false,
                    Message = "An error occurred while creating prize delivery",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}


