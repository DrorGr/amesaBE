using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Services.Interfaces;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/houses")]
    public class HousesStatsController : ControllerBase
    {
        private readonly LotteryDbContext _context;
        private readonly ILotteryService _lotteryService;
        private readonly ILogger<HousesStatsController> _logger;

        public HousesStatsController(
            LotteryDbContext context,
            ILotteryService lotteryService,
            ILogger<HousesStatsController> logger)
        {
            _context = context;
            _lotteryService = lotteryService;
            _logger = logger;
        }

        /// <summary>
        /// Get participant statistics for a house
        /// GET /api/v1/houses/{id}/participants
        /// </summary>
        [HttpGet("{id}/participants")]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<LotteryParticipantStatsDto>>> GetParticipantStats(Guid id)
        {
            try
            {
                var stats = await _lotteryService.GetParticipantStatsAsync(id);

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryParticipantStatsDto>
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryParticipantStatsDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving participant stats for house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryParticipantStatsDto>
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
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<CanEnterLotteryResponse>>> CanEnterLottery(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<CanEnterLotteryResponse>
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

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<CanEnterLotteryResponse>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can enter lottery for house {HouseId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<CanEnterLotteryResponse>
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






