using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Payment.Services.Interfaces;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Helpers;

namespace AmesaBackend.Payment.Controllers;

[ApiController]
[Route("api/v1/payments/stripe")]
public class StripePaymentController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<StripePaymentController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const int MAX_REQUEST_SIZE = 1024 * 1024; // 1MB

    public StripePaymentController(
        IStripeService stripeService,
        ILogger<StripePaymentController> logger,
        IServiceProvider serviceProvider)
    {
        _stripeService = stripeService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>Lazy-resolve rate limits so read-only endpoints do not build the payment rate limit stack.</summary>
    private IPaymentRateLimitService? TryResolvePaymentRateLimit()
    {
        try
        {
            return _serviceProvider.GetService<IPaymentRateLimitService>();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment rate limit service unavailable; continuing without per-request rate limits.");
            return null;
        }
    }

    [HttpPost("create-checkout-session")]
    [Authorize]
    [RequestSizeLimit(MAX_REQUEST_SIZE)]
    public async Task<ActionResult<ApiResponse<CheckoutSessionResponse>>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<CheckoutSessionResponse>();
            }

            var rateLimit = TryResolvePaymentRateLimit();
            if (rateLimit != null)
            {
                var canProcess = await rateLimit.CheckPaymentProcessingLimitAsync(userId);
                if (!canProcess)
                {
                    return StatusCode(429, new ApiResponse<CheckoutSessionResponse>
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

            var session = await _stripeService.CreateCheckoutSessionAsync(request, userId);

            if (rateLimit != null)
            {
                await rateLimit.IncrementPaymentProcessingAsync(userId);
            }

            return Ok(new ApiResponse<CheckoutSessionResponse> { Success = true, Data = session });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<CheckoutSessionResponse> { Success = false, Message = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new ApiResponse<CheckoutSessionResponse> { Success = false, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<CheckoutSessionResponse> { Success = false, Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<CheckoutSessionResponse> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return StatusCode(500, new ApiResponse<CheckoutSessionResponse>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "CHECKOUT_SESSION_ERROR",
                    Message = "An error occurred creating checkout session"
                }
            });
        }
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

            var rateLimit = TryResolvePaymentRateLimit();

            // Rate limiting
            if (rateLimit != null)
            {
                var canProcess = await rateLimit.CheckPaymentProcessingLimitAsync(userId);
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
            if (rateLimit != null)
            {
                await rateLimit.IncrementPaymentProcessingAsync(userId);
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








