using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Payment.DTOs;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Payment.Controllers;

/// <summary>
/// Public Stripe publishable key only. Does not use <see cref="Services.StripeService"/> or payment rate limits,
/// so unhandled DI failures on those services cannot break this endpoint.
/// </summary>
[ApiController]
[Route("api/v1/payments/stripe")]
public class StripePublishableKeyController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePublishableKeyController> _logger;

    public StripePublishableKeyController(
        IConfiguration configuration,
        ILogger<StripePublishableKeyController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("publishable-key")]
    public ActionResult<ApiResponse<StripeConfigResponse>> GetPublishableKey()
    {
        try
        {
            var publishableKey = _configuration["Stripe:PublishableKey"]
                ?? Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY")
                ?? Environment.GetEnvironmentVariable("Stripe__PublishableKey");

            if (string.IsNullOrWhiteSpace(publishableKey))
            {
                _logger.LogError("GetPublishableKey: publishable key not found in config or environment variables");
                return StatusCode(500, new ApiResponse<StripeConfigResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "CONFIGURATION_ERROR",
                        Message = "Stripe publishable key not configured"
                    }
                });
            }

            return Ok(new ApiResponse<StripeConfigResponse>
            {
                Success = true,
                Data = new StripeConfigResponse { PublishableKey = publishableKey }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe publishable key");
            return StatusCode(500, new ApiResponse<StripeConfigResponse>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred retrieving Stripe configuration"
                }
            });
        }
    }
}
