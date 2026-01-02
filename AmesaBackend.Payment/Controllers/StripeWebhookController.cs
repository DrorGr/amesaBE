using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.Services.Interfaces;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Helpers;
using System.Text;
using System.Text.Json;

namespace AmesaBackend.Payment.Controllers;

[ApiController]
[Route("api/v1/payments/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly IPaymentAuditService? _auditService;
    private const int MAX_REQUEST_SIZE = 1024 * 1024; // 1MB

    public StripeWebhookController(
        IStripeService stripeService,
        ILogger<StripeWebhookController> logger,
        IServiceProvider serviceProvider)
    {
        _stripeService = stripeService;
        _logger = logger;
        _auditService = serviceProvider.GetService<IPaymentAuditService>();
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
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Stripe webhook received without signature");
                
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

            // Extract timestamp from signature
            var timestamp = ExtractTimestampFromSignature(signature);
            if (string.IsNullOrEmpty(timestamp))
            {
                _logger.LogWarning("Stripe webhook signature missing timestamp");
                return BadRequest(new { error = "Invalid signature format" });
            }

            // Verify signature
            var isValid = await _stripeService.VerifyWebhookSignatureAsync(body, signature, timestamp);
            if (!isValid)
            {
                _logger.LogWarning("Stripe webhook signature verification failed");
                
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
            var result = await _stripeService.HandleWebhookEventAsync(eventType, eventData);

            if (!result.Processed)
            {
                _logger.LogError("Failed to process Stripe webhook event {EventType}: {Message}", eventType, result.Message);
                return StatusCode(500, new { error = "Failed to process webhook" });
            }

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string? ExtractTimestampFromSignature(string signature)
    {
        // Stripe signature format: "v1=hash,t=timestamp"
        var parts = signature.Split(',');
        foreach (var part in parts)
        {
            if (part.StartsWith("t="))
            {
                return part.Substring(2);
            }
        }
        return null;
    }
}

