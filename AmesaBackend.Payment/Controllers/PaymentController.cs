using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.DTOs;

namespace AmesaBackend.Payment.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpGet("methods")]
        public async Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>> GetPaymentMethods()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var methods = await _paymentService.GetPaymentMethodsAsync(userId);
                return Ok(new ApiResponse<List<PaymentMethodDto>> { Success = true, Data = methods });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods");
                return StatusCode(500, new ApiResponse<List<PaymentMethodDto>> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpPost("methods")]
        public async Task<ActionResult<ApiResponse<PaymentMethodDto>>> AddPaymentMethod([FromBody] AddPaymentMethodRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var method = await _paymentService.AddPaymentMethodAsync(userId, request);
                return Ok(new ApiResponse<PaymentMethodDto> { Success = true, Data = method, Message = "Payment method added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment method");
                return StatusCode(500, new ApiResponse<PaymentMethodDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpPut("methods/{id}")]
        public async Task<ActionResult<ApiResponse<PaymentMethodDto>>> UpdatePaymentMethod(Guid id, [FromBody] UpdatePaymentMethodRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var method = await _paymentService.UpdatePaymentMethodAsync(userId, id, request);
                return Ok(new ApiResponse<PaymentMethodDto> { Success = true, Data = method, Message = "Payment method updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<PaymentMethodDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment method");
                return StatusCode(500, new ApiResponse<PaymentMethodDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpDelete("methods/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeletePaymentMethod(Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                await _paymentService.DeletePaymentMethodAsync(userId, id);
                return Ok(new ApiResponse<object> { Success = true, Message = "Payment method deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment method");
                return StatusCode(500, new ApiResponse<object> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetTransactions()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var transactions = await _paymentService.GetTransactionsAsync(userId);
                return Ok(new ApiResponse<List<TransactionDto>> { Success = true, Data = transactions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, new ApiResponse<List<TransactionDto>> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpGet("transactions/{id}")]
        public async Task<ActionResult<ApiResponse<TransactionDto>>> GetTransaction(Guid id)
        {
            try
            {
                var transaction = await _paymentService.GetTransactionAsync(id);
                return Ok(new ApiResponse<TransactionDto> { Success = true, Data = transaction });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<TransactionDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction");
                return StatusCode(500, new ApiResponse<TransactionDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }

        [HttpPost("process")]
        public async Task<ActionResult<ApiResponse<PaymentResponse>>> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var response = await _paymentService.ProcessPaymentAsync(userId, request);
                return Ok(new ApiResponse<PaymentResponse> { Success = true, Data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return StatusCode(500, new ApiResponse<PaymentResponse> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message } });
            }
        }
    }
}
