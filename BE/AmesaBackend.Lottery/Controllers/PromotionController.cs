using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Shared.Contracts;

namespace AmesaBackend.Lottery.Controllers;

[ApiController]
[Route("api/v1/promotions")]
public class PromotionController : ControllerBase
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger<PromotionController> _logger;

    public PromotionController(
        IPromotionService promotionService,
        ILogger<PromotionController> logger)
    {
        _promotionService = promotionService;
        _logger = logger;
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value 
            ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        
        return userId;
    }

    /// <summary>
    /// Get all promotions with pagination and filtering
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<PagedResponse<PromotionDto>>>> GetPromotions(
        [FromQuery] PromotionSearchParams searchParams)
    {
        try
        {
            var result = await _promotionService.GetPromotionsAsync(searchParams);
            return Ok(new StandardApiResponse<PagedResponse<PromotionDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching promotions");
            return StatusCode(500, new StandardApiResponse<PagedResponse<PromotionDto>>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to fetch promotions"
                }
            });
        }
    }

    /// <summary>
    /// Get promotion by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<PromotionDto>>> GetPromotionById(Guid id)
    {
        try
        {
            var promotion = await _promotionService.GetPromotionByIdAsync(id);
            if (promotion == null)
            {
                return NotFound(new StandardApiResponse<PromotionDto>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "NOT_FOUND",
                        Message = "Promotion not found"
                    }
                });
            }

            return Ok(new StandardApiResponse<PromotionDto>
            {
                Success = true,
                Data = promotion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching promotion {PromotionId}", id);
            return StatusCode(500, new StandardApiResponse<PromotionDto>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to fetch promotion"
                }
            });
        }
    }

    /// <summary>
    /// Get promotion by code
    /// </summary>
    [HttpGet("code/{code}")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<PromotionDto>>> GetPromotionByCode(string code)
    {
        try
        {
            var promotion = await _promotionService.GetPromotionByCodeAsync(code);
            if (promotion == null)
            {
                return NotFound(new StandardApiResponse<PromotionDto>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "NOT_FOUND",
                        Message = "Promotion not found"
                    }
                });
            }

            return Ok(new StandardApiResponse<PromotionDto>
            {
                Success = true,
                Data = promotion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching promotion by code {Code}", code);
            return StatusCode(500, new StandardApiResponse<PromotionDto>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to fetch promotion"
                }
            });
        }
    }

    /// <summary>
    /// Create a new promotion
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<PromotionDto>>> CreatePromotion(
        [FromBody] CreatePromotionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var promotion = await _promotionService.CreatePromotionAsync(request, userId);
            return CreatedAtAction(
                nameof(GetPromotionById),
                new { id = promotion.Id },
                new StandardApiResponse<PromotionDto>
                {
                    Success = true,
                    Data = promotion
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promotion");
            return StatusCode(500, new StandardApiResponse<PromotionDto>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to create promotion"
                }
            });
        }
    }

    /// <summary>
    /// Update an existing promotion
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<PromotionDto>>> UpdatePromotion(
        Guid id,
        [FromBody] UpdatePromotionRequest request)
    {
        try
        {
            var promotion = await _promotionService.UpdatePromotionAsync(id, request);
            return Ok(new StandardApiResponse<PromotionDto>
            {
                Success = true,
                Data = promotion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promotion {PromotionId}", id);
            return StatusCode(500, new StandardApiResponse<PromotionDto>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to update promotion"
                }
            });
        }
    }

    /// <summary>
    /// Delete a promotion
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<bool>>> DeletePromotion(Guid id)
    {
        try
        {
            var result = await _promotionService.DeletePromotionAsync(id);
            return Ok(new StandardApiResponse<bool>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting promotion {PromotionId}", id);
            return StatusCode(500, new StandardApiResponse<bool>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to delete promotion"
                }
            });
        }
    }

    /// <summary>
    /// Validate a promotion code
    /// </summary>
    [HttpPost("validate")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<PromotionValidationResponse>>> ValidatePromotion(
        [FromBody] ValidatePromotionRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new StandardApiResponse<PromotionValidationResponse>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "AUTHENTICATION_ERROR",
                        Message = "User not authenticated"
                    }
                });
            }

            // Override userId from token
            request.UserId = userId.Value;
            var result = await _promotionService.ValidatePromotionAsync(request);
            return Ok(new StandardApiResponse<PromotionValidationResponse>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promotion");
            return StatusCode(500, new StandardApiResponse<PromotionValidationResponse>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to validate promotion"
                }
            });
        }
    }

    /// <summary>
    /// Apply a promotion code
    /// </summary>
    [HttpPost("apply")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<PromotionUsageDto>>> ApplyPromotion(
        [FromBody] ApplyPromotionRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new StandardApiResponse<PromotionUsageDto>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "AUTHENTICATION_ERROR",
                        Message = "User not authenticated"
                    }
                });
            }

            // Override userId from token
            request.UserId = userId.Value;
            var result = await _promotionService.ApplyPromotionAsync(request);
            return Ok(new StandardApiResponse<PromotionUsageDto>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying promotion");
            return StatusCode(500, new StandardApiResponse<PromotionUsageDto>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to apply promotion"
                }
            });
        }
    }

    /// <summary>
    /// Get promotion history for a user
    /// </summary>
    [HttpGet("users/{userId}/history")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<List<PromotionUsageDto>>>> GetUserPromotionHistory(Guid userId)
    {
        try
        {
            var currentUserId = GetUserId();
            if (currentUserId == null || currentUserId.Value != userId)
            {
                return Forbid();
            }

            var history = await _promotionService.GetUserPromotionHistoryAsync(userId);
            return Ok(new StandardApiResponse<List<PromotionUsageDto>>
            {
                Success = true,
                Data = history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user promotion history for {UserId}", userId);
            return StatusCode(500, new StandardApiResponse<List<PromotionUsageDto>>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to fetch user promotion history"
                }
            });
        }
    }

    /// <summary>
    /// Get available promotions for the current user
    /// </summary>
    [HttpGet("available")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<List<PromotionDto>>>> GetAvailablePromotions(
        [FromQuery] Guid? houseId = null)
    {
        if (_promotionService == null)
        {
            _logger.LogError("PromotionService is not available");
            return StatusCode(503, new StandardApiResponse<List<PromotionDto>>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "SERVICE_UNAVAILABLE",
                    Message = "Promotion service is not available"
                }
            });
        }
        
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new StandardApiResponse<List<PromotionDto>>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "AUTHENTICATION_ERROR",
                        Message = "User not authenticated"
                    }
                });
            }

            var promotions = await _promotionService.GetAvailablePromotionsAsync(userId.Value, houseId);
            return Ok(new StandardApiResponse<List<PromotionDto>>
            {
                Success = true,
                Data = promotions ?? new List<PromotionDto>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available promotions - Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                ex.GetType().Name, ex.Message, ex.StackTrace);
            return StatusCode(500, new StandardApiResponse<List<PromotionDto>>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to fetch available promotions",
                    Details = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get promotion usage statistics
    /// </summary>
    [HttpGet("{id}/stats")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<PromotionAnalyticsDto>>> GetPromotionStats(Guid id)
    {
        try
        {
            var stats = await _promotionService.GetPromotionUsageStatsAsync(id);
            return Ok(new StandardApiResponse<PromotionAnalyticsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching promotion stats for {PromotionId}", id);
            return StatusCode(500, new StandardApiResponse<PromotionAnalyticsDto>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to fetch promotion stats"
                }
            });
        }
    }

    /// <summary>
    /// Get promotion analytics
    /// </summary>
    [HttpGet("analytics")]
    [Authorize]
    public async Task<ActionResult<StandardApiResponse<List<PromotionAnalyticsDto>>>> GetPromotionAnalytics(
        [FromQuery] PromotionSearchParams? searchParams)
    {
        try
        {
            var analytics = await _promotionService.GetPromotionAnalyticsAsync(searchParams);
            return Ok(new StandardApiResponse<List<PromotionAnalyticsDto>>
            {
                Success = true,
                Data = analytics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching promotion analytics");
            return StatusCode(500, new StandardApiResponse<List<PromotionAnalyticsDto>>
            {
                Success = false,
                Error = new StandardErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "Failed to fetch promotion analytics"
                }
            });
        }
    }
}
