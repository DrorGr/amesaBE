using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services;

/// <summary>
/// Service interface for managing lottery promotions, discounts, and special offers.
/// Provides functionality for creating, updating, validating, and applying promotions to lottery tickets.
/// </summary>
public interface IPromotionService
{
    /// <summary>
    /// Retrieves a paginated list of promotions based on search parameters.
    /// </summary>
    /// <param name="searchParams">Search parameters including filters, pagination, and sorting options.</param>
    /// <returns>A paged response containing promotion DTOs matching the search criteria.</returns>
    Task<PagedResponse<PromotionDto>> GetPromotionsAsync(PromotionSearchParams searchParams);
    
    /// <summary>
    /// Retrieves a promotion by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the promotion.</param>
    /// <returns>The promotion DTO if found; otherwise, null.</returns>
    Task<PromotionDto?> GetPromotionByIdAsync(Guid id);
    
    /// <summary>
    /// Retrieves a promotion by its unique code.
    /// </summary>
    /// <param name="code">The unique promotion code (case-insensitive).</param>
    /// <returns>The promotion DTO if found; otherwise, null.</returns>
    Task<PromotionDto?> GetPromotionByCodeAsync(string code);
    
    /// <summary>
    /// Creates a new promotion with the specified details.
    /// </summary>
    /// <param name="request">The promotion creation request containing all promotion details.</param>
    /// <param name="createdBy">The unique identifier of the user creating the promotion.</param>
    /// <returns>The created promotion DTO.</returns>
    /// <exception cref="ArgumentException">Thrown when the request contains invalid data.</exception>
    Task<PromotionDto> CreatePromotionAsync(CreatePromotionRequest request, Guid? createdBy);
    
    /// <summary>
    /// Updates an existing promotion with new details.
    /// </summary>
    /// <param name="id">The unique identifier of the promotion to update.</param>
    /// <param name="request">The promotion update request containing the new details.</param>
    /// <returns>The updated promotion DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the promotion with the specified ID is not found.</exception>
    /// <exception cref="ArgumentException">Thrown when the request contains invalid data.</exception>
    Task<PromotionDto> UpdatePromotionAsync(Guid id, UpdatePromotionRequest request);
    
    /// <summary>
    /// Deletes a promotion by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the promotion to delete.</param>
    /// <returns>True if the promotion was successfully deleted; otherwise, false.</returns>
    Task<bool> DeletePromotionAsync(Guid id);
    
    /// <summary>
    /// Validates whether a promotion can be applied to a specific purchase.
    /// </summary>
    /// <param name="request">The validation request containing user, promotion, and purchase details.</param>
    /// <returns>A validation response indicating whether the promotion is valid and applicable.</returns>
    Task<PromotionValidationResponse> ValidatePromotionAsync(ValidatePromotionRequest request);
    
    /// <summary>
    /// Applies a promotion to a purchase and records the usage.
    /// </summary>
    /// <param name="request">The promotion application request containing user, promotion, and purchase details.</param>
    /// <returns>A promotion usage DTO containing the applied discount and usage information.</returns>
    /// <exception cref="ArgumentException">Thrown when the promotion cannot be applied (invalid, expired, or usage limit reached).</exception>
    Task<PromotionUsageDto> ApplyPromotionAsync(ApplyPromotionRequest request);
    
    /// <summary>
    /// Retrieves the promotion usage history for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of promotion usage DTOs representing the user's promotion history.</returns>
    Task<List<PromotionUsageDto>> GetUserPromotionHistoryAsync(Guid userId);
    
    /// <summary>
    /// Retrieves all available promotions for a user, optionally filtered by house.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="houseId">Optional house identifier to filter promotions applicable to a specific house.</param>
    /// <returns>A list of promotion DTOs that are currently available and applicable to the user.</returns>
    Task<List<PromotionDto>> GetAvailablePromotionsAsync(Guid userId, Guid? houseId);
    
    /// <summary>
    /// Retrieves usage statistics for a specific promotion.
    /// </summary>
    /// <param name="promotionId">The unique identifier of the promotion.</param>
    /// <returns>Analytics DTO containing usage statistics, revenue impact, and performance metrics.</returns>
    Task<PromotionAnalyticsDto> GetPromotionUsageStatsAsync(Guid promotionId);
    
    /// <summary>
    /// Retrieves analytics data for multiple promotions based on search parameters.
    /// </summary>
    /// <param name="searchParams">Optional search parameters to filter promotions for analytics.</param>
    /// <returns>A list of analytics DTOs containing usage statistics for matching promotions.</returns>
    Task<List<PromotionAnalyticsDto>> GetPromotionAnalyticsAsync(PromotionSearchParams? searchParams);
}
