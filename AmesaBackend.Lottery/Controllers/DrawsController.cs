using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DrawsController : ControllerBase
    {
        private readonly ILotteryService _lotteryService;
        private readonly ILogger<DrawsController> _logger;

        public DrawsController(ILotteryService lotteryService, ILogger<DrawsController> logger)
        {
            _lotteryService = lotteryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryDrawDto>>>> GetDraws()
        {
            try
            {
                var draws = await _lotteryService.GetDrawsAsync();
                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryDrawDto>> { Success = true, Data = draws });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting draws");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<List<LotteryDrawDto>> 
                { 
                    Success = false, 
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving draws." } 
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<LotteryDrawDto>>> GetDraw(Guid id)
        {
            try
            {
                var draw = await _lotteryService.GetDrawAsync(id);
                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryDrawDto> { Success = true, Data = draw });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryDrawDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting draw {DrawId}", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<LotteryDrawDto> 
                { 
                    Success = false, 
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving the draw." } 
                });
            }
        }

        [HttpGet("{id}/participants")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Service-to-service, protected by middleware
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<List<ParticipantDto>>>> GetDrawParticipants(Guid id)
        {
            try
            {
                var participants = await _lotteryService.GetDrawParticipantsAsync(id);
                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<List<ParticipantDto>> 
                { 
                    Success = true, 
                    Data = participants 
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<List<ParticipantDto>> 
                { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting draw participants");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<List<ParticipantDto>> 
                { 
                    Success = false, 
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving draw participants" } 
                });
            }
        }

        [HttpPost("{id}/conduct")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<object>>> ConductDraw(Guid id, [FromBody] ConductDrawRequest request)
        {
            try
            {
                await _lotteryService.ConductDrawAsync(id, request);
                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<object> { Success = true, Message = "Draw conducted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error conducting draw");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<object> 
                { 
                    Success = false, 
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred conducting the draw." } 
                });
            }
        }
    }
}

