using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Shared.Helpers;
using PaymentHelpers = AmesaBackend.Payment.Helpers;

namespace AmesaBackend.Payment.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRateLimitService? _rateLimitService;
    private readonly ILogger<PaymentController> _logger;
    private const int MAX_REQUEST_SIZE = 1024 * 1024; // 1MB

    public PaymentController(
        IPaymentService paymentService,
        ILogger<PaymentController> logger,
        IServiceProvider serviceProvider)
    {
        _paymentService = paymentService;
        _logger = logger;
        _rateLimitService = serviceProvider.GetService<IPaymentRateLimitService>();
    }

        [HttpGet("methods")]
        public async Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>> GetPaymentMethods()
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<PaymentMethodDto>> 
                    { 
                        Success = false, 
                        Message = "Authentication required" 
                    });
                }
                var methods = await _paymentService.GetPaymentMethodsAsync(userId);
                return Ok(new ApiResponse<List<PaymentMethodDto>> { Success = true, Data = methods });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods");
                return StatusCode(500, new ApiResponse<List<PaymentMethodDto>> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving payment methods" } });
            }
        }

        [HttpPost("methods")]
        public async Task<ActionResult<ApiResponse<PaymentMethodDto>>> AddPaymentMethod([FromBody] AddPaymentMethodRequest request)
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<PaymentMethodDto> 
                    { 
                        Success = false, 
                        Message = "Authentication required" 
                    });
                }
                var method = await _paymentService.AddPaymentMethodAsync(userId, request);
                return Ok(new ApiResponse<PaymentMethodDto> { Success = true, Data = method, Message = "Payment method added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment method");
                return StatusCode(500, new ApiResponse<PaymentMethodDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred adding payment method" } });
            }
        }

        [HttpPut("methods/{id}")]
        public async Task<ActionResult<ApiResponse<PaymentMethodDto>>> UpdatePaymentMethod(Guid id, [FromBody] UpdatePaymentMethodRequest request)
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<PaymentMethodDto> 
                    { 
                        Success = false, 
                        Message = "Authentication required" 
                    });
                }
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
                return StatusCode(500, new ApiResponse<PaymentMethodDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred updating payment method" } });
            }
        }

        [HttpDelete("methods/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeletePaymentMethod(Guid id)
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = "Authentication required" 
                    });
                }
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
                return StatusCode(500, new ApiResponse<object> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred deleting payment method" } });
            }
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetTransactions()
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<List<TransactionDto>> 
                    { 
                        Success = false, 
                        Message = "Authentication required" 
                    });
                }
                var transactions = await _paymentService.GetTransactionsAsync(userId);
                return Ok(new ApiResponse<List<TransactionDto>> { Success = true, Data = transactions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, new ApiResponse<List<TransactionDto>> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving transactions" } });
            }
        }

        [HttpGet("transactions/{id}")]
        public async Task<ActionResult<ApiResponse<TransactionDto>>> GetTransaction(Guid id)
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return PaymentHelpers.ControllerHelpers.UnauthorizedResponse<TransactionDto>();
                }

                var transaction = await _paymentService.GetTransactionAsync(id, userId);
                return Ok(new ApiResponse<TransactionDto> { Success = true, Data = transaction });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<TransactionDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction");
                return StatusCode(500, new ApiResponse<TransactionDto> { Success = false, Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving transaction" } });
            }
        }

        [HttpPost("process")]
        public async Task<ActionResult<ApiResponse<PaymentResponse>>> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            try
            {
                if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<PaymentResponse> 
                    { 
                        Success = false, 
                        Message = "Authentication required" 
                    });
                }
                var response = await _paymentService.ProcessPaymentAsync(userId, request);
                return Ok(new ApiResponse<PaymentResponse> { Success = true, Data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return StatusCode(500, new ApiResponse<PaymentResponse> { Success = false, Error = new ErrorResponse { Code = "PAYMENT_PROCESSING_ERROR", Message = "An error occurred processing your payment. Please try again." } });
            }
        }

        [HttpPost("refund")]
        public async Task<ActionResult<ApiResponse<RefundResponse>>> RefundPayment([FromBody] RefundRequest request)
        {
            try
            {
                Guid userId;
                
                // Check if this is a service-to-service call (has X-Service-Auth header)
                var serviceAuthHeader = Request.Headers["X-Service-Auth"].FirstOrDefault();
                var isServiceCall = !string.IsNullOrEmpty(serviceAuthHeader) && 
                                     serviceAuthHeader == Environment.GetEnvironmentVariable("SERVICE_AUTH_API_KEY");

                if (isServiceCall)
                {
                    // For service-to-service calls, get userId from the transaction
                    // This allows Lottery service to refund on behalf of users
                    var transaction = await _paymentService.GetTransactionAsync(request.TransactionId, Guid.Empty);
                    if (transaction == null)
                    {
                        return NotFound(new ApiResponse<RefundResponse> { Success = false, Message = "Transaction not found" });
                    }
                    // Get userId from transaction - we'll need to modify GetTransactionAsync or create a new method
                    // For now, we'll query directly
                    userId = await _paymentService.GetTransactionUserIdAsync(request.TransactionId) ?? Guid.Empty;
                    if (userId == Guid.Empty)
                    {
                        return NotFound(new ApiResponse<RefundResponse> { Success = false, Message = "Transaction not found" });
                    }
                }
                else
                {
                    // Regular user call - get userId from JWT token
                    if (!AmesaBackend.Shared.Helpers.ControllerHelpers.TryGetUserId(User, out userId))
                    {
                        return Unauthorized(new ApiResponse<RefundResponse> 
                        { 
                            Success = false, 
                            Message = "Authentication required" 
                        });
                    }

                    // Check if user is admin (for admin-initiated refunds)
                    var isAdmin = User.IsInRole("Admin");
                    if (!isAdmin)
                    {
                        // For non-admin users, verify they own the transaction
                        // This is handled in the service, but we can add additional checks here if needed
                    }
                }

                var response = await _paymentService.RefundPaymentAsync(userId, request);
                return Ok(new ApiResponse<RefundResponse> { Success = true, Data = response, Message = "Refund initiated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<RefundResponse> { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<RefundResponse> { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<RefundResponse> { Success = false, Message = ex.Message });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new ApiResponse<RefundResponse> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund");
                return StatusCode(500, new ApiResponse<RefundResponse> { Success = false, Error = new ErrorResponse { Code = "REFUND_PROCESSING_ERROR", Message = "An error occurred processing your refund. Please try again." } });
            }
        }
    }
}
