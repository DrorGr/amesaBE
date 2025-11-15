using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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
    }
}

