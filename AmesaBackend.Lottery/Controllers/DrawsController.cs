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
        public async Task<ActionResult<ApiResponse<List<LotteryDrawDto>>>> GetDraws()
        {
            try
            {
                var draws = await _lotteryService.GetDrawsAsync();
                return Ok(new ApiResponse<List<LotteryDrawDto>> { Success = true, Data = draws });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting draws");
                return StatusCode(500, new ApiResponse<List<LotteryDrawDto>> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<LotteryDrawDto>>> GetDraw(Guid id)
        {
            try
            {
                var draw = await _lotteryService.GetDrawAsync(id);
                return Ok(new ApiResponse<LotteryDrawDto> { Success = true, Data = draw });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<LotteryDrawDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting draw");
                return StatusCode(500, new ApiResponse<LotteryDrawDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpPost("{id}/conduct")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> ConductDraw(Guid id, [FromBody] ConductDrawRequest request)
        {
            try
            {
                await _lotteryService.ConductDrawAsync(id, request);
                return Ok(new ApiResponse<object> { Success = true, Message = "Draw conducted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error conducting draw");
                return StatusCode(500, new ApiResponse<object> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }
    }
}

