using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Auth.Services;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/promotions")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
        private readonly ILogger<PromotionController> _logger;
        private readonly IRateLimitService? _rateLimitService;

        public PromotionController(
            IPromotionService promotionService,
            ILogger<PromotionController> logger,
            IRateLimitService? rateLimitService = null)
        {
            _promotionService = promotionService;
            _logger = logger;
            _rateLimitService = rateLimitService;
        }

        /// <summary>
        /// Get list of promotions with filtering and pagination
        /// GET /api/v1/promotions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<PromotionDto>>>> GetPromotions(
            [FromQuery] PromotionSearchParams searchParams)
        {
            try
            {
                var result = await _promotionService.GetPromotionsAsync(searchParams);
                return Ok(new ApiResponse<PagedResponse<PromotionDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Promotions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving promotions");
                return StatusCode(500, new ApiResponse<PagedResponse<PromotionDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving promotions",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving promotions." }
                });
            }
        }

        /// <summary>
        /// Get promotion by ID
        /// GET /api/v1/promotions/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PromotionDto>>> GetPromotionById(Guid id)
        {
            try
            {
                var promotion = await _promotionService.GetPromotionByIdAsync(id);
                if (promotion == null)
                {
                    return NotFound(new ApiResponse<PromotionDto>
                    {
                        Success = false,
                        Message = "Promotion not found",
                        Error = new ErrorResponse { Code = "PROMOTION_NOT_FOUND" }
                    });
                }

                return Ok(new ApiResponse<PromotionDto>
                {
                    Success = true,
                    Data = promotion,
                    Message = "Promotion retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving promotion {PromotionId}", id);
                return StatusCode(500, new ApiResponse<PromotionDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving promotion",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving the promotion." }
                });
            }
        }

        /// <summary>
        /// Get promotion by code
        /// GET /api/v1/promotions/code/{code}
        /// </summary>
        [HttpGet("code/{code}")]
        public async Task<ActionResult<ApiResponse<PromotionDto>>> GetPromotionByCode(string code)
        {
            try
            {
                var promotion = await _promotionService.GetPromotionByCodeAsync(code);
                if (promotion == null)
                {
                    return NotFound(new ApiResponse<PromotionDto>
                    {
                        Success = false,
                        Message = "Promotion not found",
                        Error = new ErrorResponse { Code = "PROMOTION_NOT_FOUND" }
                    });
                }

                return Ok(new ApiResponse<PromotionDto>
                {
                    Success = true,
                    Data = promotion,
                    Message = "Promotion retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving promotion by code {Code}", code);
                return StatusCode(500, new ApiResponse<PromotionDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving promotion",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving the promotion." }
                });
            }
        }

        /// <summary>
        /// Create a new promotion (Admin only)
        /// POST /api/v1/promotions
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PromotionDto>>> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            try
            {
                // Get user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Guid? createdBy = null;
                if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
                {
                    createdBy = userId;
                }

                var promotion = await _promotionService.CreatePromotionAsync(request, createdBy);
                return CreatedAtAction(
                    nameof(GetPromotionById),
                    new { id = promotion.Id },
                    new ApiResponse<PromotionDto>
                    {
                        Success = true,
                        Data = promotion,
                        Message = "Promotion created successfully"
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<PromotionDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Error = new ErrorResponse { Code = "PROMOTION_CODE_INVALID" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion");
                return StatusCode(500, new ApiResponse<PromotionDto>
                {
                    Success = false,
                    Message = "An error occurred while creating promotion",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred creating the promotion." }
                });
            }
        }

        /// <summary>
        /// Update a promotion (Admin only)
        /// PUT /api/v1/promotions/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PromotionDto>>> UpdatePromotion(
            Guid id,
            [FromBody] UpdatePromotionRequest request)
        {
            try
            {
                var promotion = await _promotionService.UpdatePromotionAsync(id, request);
                return Ok(new ApiResponse<PromotionDto>
                {
                    Success = true,
                    Data = promotion,
                    Message = "Promotion updated successfully"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<PromotionDto>
                {
                    Success = false,
                    Message = "Promotion not found",
                    Error = new ErrorResponse { Code = "PROMOTION_NOT_FOUND" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promotion {PromotionId}", id);
                return StatusCode(500, new ApiResponse<PromotionDto>
                {
                    Success = false,
                    Message = "An error occurred while updating promotion",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred updating the promotion." }
                });
            }
        }

        /// <summary>
        /// Delete a promotion (Admin only) - Soft delete
        /// DELETE /api/v1/promotions/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeletePromotion(Guid id)
        {
            try
            {
                var deleted = await _promotionService.DeletePromotionAsync(id);
                if (!deleted)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Promotion not found",
                        Error = new ErrorResponse { Code = "PROMOTION_NOT_FOUND" }
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Promotion deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting promotion {PromotionId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting promotion",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Validate a promotion code
        /// POST /api/v1/promotions/validate
        /// </summary>
        [HttpPost("validate")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PromotionValidationResponse>>> ValidatePromotion(
            [FromBody] ValidatePromotionRequest request)
        {
            try
            {
                // Get user ID from claims and override request userId for security
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
                {
                    return Unauthorized(new ApiResponse<PromotionValidationResponse>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Error = new ErrorResponse { Code = "UNAUTHORIZED" }
                    });
                }

                // Override userId from request with authenticated user ID for security
                request.UserId = authenticatedUserId;

                var validation = await _promotionService.ValidatePromotionAsync(request);
                return Ok(new ApiResponse<PromotionValidationResponse>
                {
                    Success = true,
                    Data = validation,
                    Message = validation.IsValid ? "Promotion is valid" : validation.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promotion code {Code}", request.Code);
                return StatusCode(500, new ApiResponse<PromotionValidationResponse>
                {
                    Success = false,
                    Message = "An error occurred while validating promotion",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred validating the promotion." }
                });
            }
        }

        /// <summary>
        /// Apply a promotion (record usage)
        /// POST /api/v1/promotions/apply
        /// </summary>
        [HttpPost("apply")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PromotionUsageDto>>> ApplyPromotion(
            [FromBody] ApplyPromotionRequest request)
        {
            try
            {
                // Get user ID from claims and verify it matches request
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<PromotionUsageDto>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Error = new ErrorResponse { Code = "UNAUTHORIZED" }
                    });
                }

                // Ensure user can only apply promotions for themselves
                if (userId != request.UserId)
                {
                    return Forbid();
                }

                var usage = await _promotionService.ApplyPromotionAsync(request);
                return Ok(new ApiResponse<PromotionUsageDto>
                {
                    Success = true,
                    Data = usage,
                    Message = "Promotion applied successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<PromotionUsageDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Error = new ErrorResponse { Code = "PROMOTION_NOT_FOUND" }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<PromotionUsageDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Error = new ErrorResponse { Code = "PROMOTION_VALIDATION_FAILED" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying promotion {Code}", request.Code);
                return StatusCode(500, new ApiResponse<PromotionUsageDto>
                {
                    Success = false,
                    Message = "An error occurred while applying promotion",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred applying the promotion." }
                });
            }
        }

        /// <summary>
        /// Get user promotion history
        /// GET /api/v1/promotions/users/{userId}/history
        /// </summary>
        [HttpGet("users/{userId}/history")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<PromotionUsageDto>>>> GetUserPromotionHistory(Guid userId)
        {
            try
            {
                // Get user ID from claims and verify it matches request
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
                {
                    return Unauthorized(new ApiResponse<List<PromotionUsageDto>>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Error = new ErrorResponse { Code = "UNAUTHORIZED" }
                    });
                }

                // Users can only view their own history (unless admin)
                if (authenticatedUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var history = await _promotionService.GetUserPromotionHistoryAsync(userId);
                return Ok(new ApiResponse<List<PromotionUsageDto>>
                {
                    Success = true,
                    Data = history,
                    Message = "Promotion history retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving promotion history for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<List<PromotionUsageDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving promotion history",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Get available promotions for current user
        /// GET /api/v1/promotions/available
        /// </summary>
        [HttpGet("available")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<PromotionDto>>>> GetAvailablePromotions(
            [FromQuery] Guid? houseId)
        {
            Guid? userId = null;
            try
            {
                // Get user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var parsedUserId))
                {
                    return Unauthorized(new ApiResponse<List<PromotionDto>>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Error = new ErrorResponse { Code = "UNAUTHORIZED" }
                    });
                }
                userId = parsedUserId;

                var promotions = await _promotionService.GetAvailablePromotionsAsync(userId.Value, houseId);
                return Ok(new ApiResponse<List<PromotionDto>>
                {
                    Success = true,
                    Data = promotions,
                    Message = "Available promotions retrieved successfully"
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving available promotions for user {UserId}", userId);
                return StatusCode(503, new ApiResponse<List<PromotionDto>>
                {
                    Success = false,
                    Message = "Service is temporarily unavailable. Please try again later.",
                    Error = new ErrorResponse { Code = "SERVICE_UNAVAILABLE", Message = "Service is temporarily unavailable" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available promotions for user {UserId}: {Message}", userId, ex.Message);
                return StatusCode(500, new ApiResponse<List<PromotionDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving available promotions",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Get promotion usage statistics (Admin only)
        /// GET /api/v1/promotions/{id}/stats
        /// </summary>
        [HttpGet("{id}/stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PromotionAnalyticsDto>>> GetPromotionStats(Guid id)
        {
            try
            {
                var stats = await _promotionService.GetPromotionUsageStatsAsync(id);
                return Ok(new ApiResponse<PromotionAnalyticsDto>
                {
                    Success = true,
                    Data = stats,
                    Message = "Promotion statistics retrieved successfully"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<PromotionAnalyticsDto>
                {
                    Success = false,
                    Message = "Promotion not found",
                    Error = new ErrorResponse { Code = "PROMOTION_NOT_FOUND" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving promotion stats for {PromotionId}", id);
                return StatusCode(500, new ApiResponse<PromotionAnalyticsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving promotion statistics",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred retrieving promotion statistics." }
                });
            }
        }

        /// <summary>
        /// Get promotion analytics (Admin only)
        /// GET /api/v1/promotions/analytics
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<PromotionAnalyticsDto>>>> GetPromotionAnalytics(
            [FromQuery] PromotionSearchParams? searchParams)
        {
            try
            {
                var analytics = await _promotionService.GetPromotionAnalyticsAsync(searchParams);
                return Ok(new ApiResponse<List<PromotionAnalyticsDto>>
                {
                    Success = true,
                    Data = analytics,
                    Message = "Promotion analytics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving promotion analytics");
                return StatusCode(500, new ApiResponse<List<PromotionAnalyticsDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving promotion analytics",
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }
    }
}

