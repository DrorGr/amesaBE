using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Helpers;

namespace AmesaBackend.Payment.Controllers;

[ApiController]
[Route("api/v1/payments/stripe")]
public class StripePaymentController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<StripePaymentController> _logger;
    private readonly IPaymentRateLimitService? _rateLimitService;
    private const int MAX_REQUEST_SIZE = 1024 * 1024; // 1MB

    public StripePaymentController(
        IStripeService stripeService,
        ILogger<StripePaymentController> logger,
        IServiceProvider serviceProvider)
    {
        _stripeService = stripeService;
        _logger = logger;
        _rateLimitService = serviceProvider.GetService<IPaymentRateLimitService>();
    }

    [HttpPost("create-payment-intent")]
    [Authorize]
    [RequestSizeLimit(MAX_REQUEST_SIZE)]
    public async Task<ActionResult<ApiResponse<PaymentIntentResponse>>> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<PaymentIntentResponse>();
            }

            // Rate limiting
            if (_rateLimitService != null)
            {
                var canProcess = await _rateLimitService.CheckPaymentProcessingLimitAsync(userId);
                if (!canProcess)
                {
                    return StatusCode(429, new ApiResponse<PaymentIntentResponse> 
                    { 
                        Success = false, 
                        Error = new ErrorResponse 
                        { 
                            Code = "RATE_LIMIT_EXCEEDED", 
                            Message = "Too many payment requests. Please try again later." 
                        } 
                    });
                }
            }

            var paymentIntent = await _stripeService.CreatePaymentIntentAsync(request, userId);

            // Increment rate limit counter
            if (_rateLimitService != null)
            {
                await _rateLimitService.IncrementPaymentProcessingAsync(userId);
            }

            return Ok(new ApiResponse<PaymentIntentResponse> { Success = true, Data = paymentIntent });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<PaymentIntentResponse> { Success = false, Message = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new ApiResponse<PaymentIntentResponse> { Success = false, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<PaymentIntentResponse> { Success = false, Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<PaymentIntentResponse> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return StatusCode(500, new ApiResponse<PaymentIntentResponse> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "PAYMENT_INTENT_ERROR", 
                    Message = "An error occurred creating payment intent" 
                } 
            });
        }
    }

    [HttpPost("confirm-payment-intent")]
    [Authorize]
    [RequestSizeLimit(MAX_REQUEST_SIZE)]
    public async Task<ActionResult<ApiResponse<PaymentIntentResponse>>> ConfirmPaymentIntent([FromBody] ConfirmPaymentIntentRequest request)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<PaymentIntentResponse>();
            }

            var paymentIntent = await _stripeService.ConfirmPaymentIntentAsync(request, userId);
            return Ok(new ApiResponse<PaymentIntentResponse> { Success = true, Data = paymentIntent });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<PaymentIntentResponse> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment intent");
            return StatusCode(500, new ApiResponse<PaymentIntentResponse> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "PAYMENT_CONFIRMATION_ERROR", 
                    Message = "An error occurred confirming payment" 
                } 
            });
        }
    }

    [HttpPost("create-setup-intent")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<SetupIntentResponse>>> CreateSetupIntent()
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<SetupIntentResponse>();
            }

            var setupIntent = await _stripeService.CreateSetupIntentAsync(userId);
            return Ok(new ApiResponse<SetupIntentResponse> { Success = true, Data = setupIntent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating setup intent");
            return StatusCode(500, new ApiResponse<SetupIntentResponse> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "SETUP_INTENT_ERROR", 
                    Message = "An error occurred creating setup intent" 
                } 
            });
        }
    }

    [HttpGet("payment-intent/{paymentIntentId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaymentIntentResponse>>> GetPaymentIntent(string paymentIntentId)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<PaymentIntentResponse>();
            }

            var paymentIntent = await _stripeService.GetPaymentIntentAsync(paymentIntentId);
            if (paymentIntent == null)
            {
                return NotFound(new ApiResponse<PaymentIntentResponse> { Success = false, Message = "Payment intent not found" });
            }

            return Ok(new ApiResponse<PaymentIntentResponse> { Success = true, Data = paymentIntent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment intent {PaymentIntentId}", paymentIntentId);
            return StatusCode(500, new ApiResponse<PaymentIntentResponse> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "INTERNAL_ERROR", 
                    Message = "An error occurred retrieving payment intent" 
                } 
            });
        }
    }
}

