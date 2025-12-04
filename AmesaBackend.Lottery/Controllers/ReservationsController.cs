using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Shared.Helpers;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly ITicketReservationService _reservationService;
        private readonly IRedisInventoryManager _inventoryManager;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(
            ITicketReservationService reservationService,
            IRedisInventoryManager inventoryManager,
            ILogger<ReservationsController> logger)
        {
            _reservationService = reservationService;
            _inventoryManager = inventoryManager;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>>> CreateReservation(
            [FromBody] CreateReservationRequest request,
            [FromQuery] Guid houseId)
        {
            // Prevent caching of reservation endpoints
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");
            
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                
                var reservation = await _reservationService.CreateReservationAsync(
                    request, 
                    houseId, 
                    userId);

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
                _logger.LogError(ex, "Error creating reservation for house {HouseId}", houseId);
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

        [HttpGet("{id}")]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>>> GetReservation(Guid id)
        {
            // Prevent caching
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");
            
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                
                var reservation = await _reservationService.GetReservationAsync(id, userId);
                
                if (reservation == null)
                {
                    return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "NOT_FOUND",
                            Message = "Reservation not found"
                        }
                    });
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                {
                    Success = true,
                    Data = reservation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation {ReservationId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<ReservationDto>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred retrieving your reservation."
                    }
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<object>>> CancelReservation(Guid id)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                
                var cancelled = await _reservationService.CancelReservationAsync(id, userId);
                
                if (!cancelled)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "INVALID_OPERATION",
                            Message = "Unable to cancel reservation. It may have already been processed."
                        }
                    });
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = true,
                    Data = new { message = "Reservation cancelled successfully" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation {ReservationId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred cancelling your reservation."
                    }
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<List<ReservationDto>>>> GetUserReservations(
            [FromQuery] string? status = null,
            [FromQuery] int? page = null,
            [FromQuery] int? limit = null)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<List<ReservationDto>>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                
                var reservations = await _reservationService.GetUserReservationsAsync(userId, status, page, limit);

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<List<ReservationDto>>
                {
                    Success = true,
                    Data = reservations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user reservations");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<List<ReservationDto>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred retrieving your reservations."
                    }
                });
            }
        }
    }
}

