using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Shared.Contracts;

namespace AmesaBackend.Lottery.Controllers;

/// <summary>
/// Controller for managing lottery promotions and discounts.
/// Provides endpoints for creating, retrieving, updating, validating, and applying promotions to lottery purchases.
/// </summary>
[ApiController]
[Route("api/v1/promotions")]
public class PromotionController : ControllerBase
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger<PromotionController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionController"/> class.
    /// </summary>
    /// <param name="promotionService">Service for managing promotions.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public PromotionController(
        IPromotionService promotionService,
        ILogger<PromotionController> logger)
    {
        _promotionService = promotionService;
        _logger = logger;
    }

    /// <summary>
    /// Extracts the user ID from the current authentication token, if available.
    /// </summary>
    /// <returns>The user's unique identifier as a Guid if authenticated; otherwise, null.</returns>
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
    /// Retrieves a paginated list of promotions with optional filtering and search capabilities.
    /// </summary>
    /// <param name="searchParams">Search parameters including filters, pagination, and sorting options.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Paged response with promotion DTOs
    /// </returns>
    /// <response code="200">Successfully retrieved promotions.</response>
    /// <response code="401">User is not authenticated (when Authorize attribute is applied).</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Retrieves a specific promotion by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the promotion to retrieve.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Promotion DTO if found
    /// - Error: Error details if promotion not found
    /// </returns>
    /// <response code="200">Successfully retrieved promotion.</response>
    /// <response code="404">Promotion with the specified ID was not found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Retrieves a specific promotion by its unique code.
    /// </summary>
    /// <param name="code">The unique promotion code (case-insensitive).</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Promotion DTO if found
    /// - Error: Error details if promotion not found
    /// </returns>
    /// <response code="200">Successfully retrieved promotion.</response>
    /// <response code="404">Promotion with the specified code was not found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Creates a new promotion with the specified details.
    /// </summary>
    /// <param name="request">The promotion creation request containing all promotion details (title, type, discount, dates, etc.).</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Created promotion DTO
    /// - Error: Error details if creation fails
    /// </returns>
    /// <response code="201">Successfully created promotion.</response>
    /// <response code="400">Invalid request data provided.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Updates an existing promotion with new details.
    /// </summary>
    /// <param name="id">The unique identifier of the promotion to update.</param>
    /// <param name="request">The promotion update request containing the new details.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Updated promotion DTO
    /// - Error: Error details if update fails
    /// </returns>
    /// <response code="200">Successfully updated promotion.</response>
    /// <response code="400">Invalid request data provided.</response>
    /// <response code="404">Promotion with the specified ID was not found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Deletes a promotion by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the promotion to delete.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Boolean indicating whether the promotion was deleted
    /// - Error: Error details if deletion fails
    /// </returns>
    /// <response code="200">Successfully deleted promotion (or promotion not found).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Validates whether a promotion can be applied to a specific purchase.
    /// Checks promotion validity, expiration, usage limits, and applicability to the user and purchase.
    /// </summary>
    /// <param name="request">The validation request containing user, promotion, and purchase details.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Validation response with validity status, discount amount, and applicable restrictions
    /// - Error: Error details if validation fails
    /// </returns>
    /// <response code="200">Successfully validated promotion.</response>
    /// <response code="400">Invalid request data provided.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Applies a promotion to a purchase and records the usage.
    /// This endpoint applies the discount and creates a usage record for tracking and analytics.
    /// </summary>
    /// <param name="request">The promotion application request containing user, promotion, and purchase details.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Promotion usage DTO with applied discount and usage information
    /// - Error: Error details if application fails (e.g., promotion invalid, expired, or usage limit reached)
    /// </returns>
    /// <response code="200">Successfully applied promotion.</response>
    /// <response code="400">Promotion cannot be applied (invalid, expired, or usage limit reached).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Retrieves the promotion usage history for a specific user.
    /// Users can only access their own promotion history.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose promotion history to retrieve.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: List of promotion usage DTOs representing the user's promotion history
    /// - Error: Error details if retrieval fails
    /// </returns>
    /// <response code="200">Successfully retrieved user promotion history.</response>
    /// <response code="403">User attempted to access another user's promotion history.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Retrieves all available promotions for the authenticated user.
    /// Returns promotions that are currently active, not expired, and applicable to the user (optionally filtered by house).
    /// </summary>
    /// <param name="houseId">Optional house identifier to filter promotions applicable to a specific house.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: List of promotion DTOs that are currently available and applicable to the user
    /// - Error: Error details if retrieval fails
    /// </returns>
    /// <response code="200">Successfully retrieved available promotions.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="503">Promotion service is not available.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Retrieves usage statistics and analytics for a specific promotion.
    /// Includes metrics such as total uses, unique users, revenue impact, and performance data.
    /// </summary>
    /// <param name="id">The unique identifier of the promotion.</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: Analytics DTO containing usage statistics, revenue impact, and performance metrics
    /// - Error: Error details if retrieval fails
    /// </returns>
    /// <response code="200">Successfully retrieved promotion statistics.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
    /// Retrieves analytics data for multiple promotions based on search parameters.
    /// Provides aggregated analytics including usage statistics, revenue impact, and performance metrics for matching promotions.
    /// </summary>
    /// <param name="searchParams">Optional search parameters to filter promotions for analytics (filters, pagination, sorting).</param>
    /// <returns>
    /// A standard API response containing:
    /// - Success: Boolean indicating operation success
    /// - Data: List of analytics DTOs containing usage statistics for matching promotions
    /// - Error: Error details if retrieval fails
    /// </returns>
    /// <response code="200">Successfully retrieved promotion analytics.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">An error occurred while processing the request.</response>
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
