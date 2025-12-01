using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Helpers;
using System.Text;
using System.Text.Json;

namespace AmesaBackend.Payment.Controllers;

[ApiController]
[Route("api/v1/payments/crypto")]
public class CryptoPaymentController : ControllerBase
{
    private readonly ICoinbaseCommerceService _coinbaseService;
    private readonly ILogger<CryptoPaymentController> _logger;
    private readonly IPaymentAuditService? _auditService;
    private const int MAX_REQUEST_SIZE = 1024 * 1024; // 1MB

    public CryptoPaymentController(
        ICoinbaseCommerceService coinbaseService,
        ILogger<CryptoPaymentController> logger,
        IServiceProvider serviceProvider)
    {
        _coinbaseService = coinbaseService;
        _logger = logger;
        _auditService = serviceProvider.GetService<IPaymentAuditService>();
    }

    [HttpPost("create-charge")]
    [Authorize]
    [RequestSizeLimit(MAX_REQUEST_SIZE)]
    public async Task<ActionResult<ApiResponse<CoinbaseChargeResponse>>> CreateCharge([FromBody] CreateCryptoChargeRequest request)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<CoinbaseChargeResponse>();
            }

            var charge = await _coinbaseService.CreateChargeAsync(request, userId);
            return Ok(new ApiResponse<CoinbaseChargeResponse> { Success = true, Data = charge });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<CoinbaseChargeResponse> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating crypto charge");
            return StatusCode(500, new ApiResponse<CoinbaseChargeResponse> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "CHARGE_CREATION_ERROR", 
                    Message = "An error occurred creating crypto payment charge" 
                } 
            });
        }
    }

    [HttpGet("charge/{chargeId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CoinbaseChargeResponse>>> GetCharge(string chargeId)
    {
        try
        {
            if (!ControllerHelpers.TryGetUserId(User, out var userId))
            {
                return ControllerHelpers.UnauthorizedResponse<CoinbaseChargeResponse>();
            }

            var charge = await _coinbaseService.GetChargeAsync(chargeId);
            if (charge == null)
            {
                return NotFound(new ApiResponse<CoinbaseChargeResponse> { Success = false, Message = "Charge not found" });
            }

            return Ok(new ApiResponse<CoinbaseChargeResponse> { Success = true, Data = charge });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting crypto charge {ChargeId}", chargeId);
            return StatusCode(500, new ApiResponse<CoinbaseChargeResponse> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "INTERNAL_ERROR", 
                    Message = "An error occurred retrieving charge" 
                } 
            });
        }
    }

    [HttpGet("supported-cryptocurrencies")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<SupportedCrypto>>>> GetSupportedCryptocurrencies()
    {
        try
        {
            var cryptos = await _coinbaseService.GetSupportedCryptocurrenciesAsync();
            return Ok(new ApiResponse<List<SupportedCrypto>> { Success = true, Data = cryptos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported cryptocurrencies");
            return StatusCode(500, new ApiResponse<List<SupportedCrypto>> 
            { 
                Success = false, 
                Error = new ErrorResponse 
                { 
                    Code = "INTERNAL_ERROR", 
                    Message = "An error occurred retrieving supported cryptocurrencies" 
                } 
            });
        }
    }

    [HttpPost("webhook")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [RequestSizeLimit(MAX_REQUEST_SIZE)]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            // Read raw body for signature verification
            Request.EnableBuffering();
            var body = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync();
            Request.Body.Position = 0;

            // Get signature from header
            var signature = Request.Headers["X-CC-Webhook-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Coinbase Commerce webhook received without signature");
                
                // Audit log
                if (_auditService != null)
                {
                    await _auditService.LogActionAsync(
                        Guid.Empty,
                        "webhook_signature_missing",
                        "webhook",
                        null,
                        null,
                        null,
                        ControllerHelpers.GetIpAddress(HttpContext),
                        ControllerHelpers.GetUserAgent(HttpContext),
                        null);
                }

                return BadRequest(new { error = "Missing signature" });
            }

            // Verify signature
            var isValid = await _coinbaseService.VerifyWebhookSignatureAsync(body, signature);
            if (!isValid)
            {
                _logger.LogWarning("Coinbase Commerce webhook signature verification failed");
                
                // Audit log
                if (_auditService != null)
                {
                    await _auditService.LogActionAsync(
                        Guid.Empty,
                        "webhook_signature_failed",
                        "webhook",
                        null,
                        null,
                        null,
                        ControllerHelpers.GetIpAddress(HttpContext),
                        ControllerHelpers.GetUserAgent(HttpContext),
                        new Dictionary<string, object> { ["Signature"] = signature });
                }

                return Unauthorized(new { error = "Invalid signature" });
            }

            // Parse event
            var eventData = JsonSerializer.Deserialize<JsonElement>(body);
            var eventType = eventData.GetProperty("type").GetString();
            
            if (string.IsNullOrEmpty(eventType))
            {
                return BadRequest(new { error = "Missing event type" });
            }

            // Audit log - webhook received
            if (_auditService != null)
            {
                await _auditService.LogActionAsync(
                    Guid.Empty,
                    "webhook_received",
                    "webhook",
                    null,
                    null,
                    null,
                    ControllerHelpers.GetIpAddress(HttpContext),
                    ControllerHelpers.GetUserAgent(HttpContext),
                    new Dictionary<string, object> { ["EventType"] = eventType });
            }

            // Handle event
            var result = await _coinbaseService.HandleWebhookEventAsync(eventType, eventData);

            if (!result.Processed)
            {
                _logger.LogError("Failed to process Coinbase Commerce webhook event {EventType}: {Message}", eventType, result.Message);
                return StatusCode(500, new { error = "Failed to process webhook" });
            }

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Coinbase Commerce webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

