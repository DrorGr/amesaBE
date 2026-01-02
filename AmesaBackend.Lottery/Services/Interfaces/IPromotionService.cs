using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services.Interfaces
{
    public interface IPromotionService
    {
        Task<PagedResponse<PromotionDto>> GetPromotionsAsync(PromotionSearchParams searchParams);
        Task<PromotionDto?> GetPromotionByIdAsync(Guid id);
        Task<PromotionDto?> GetPromotionByCodeAsync(string code);
        Task<PromotionDto> CreatePromotionAsync(CreatePromotionRequest request, Guid? createdBy);
        Task<PromotionDto> UpdatePromotionAsync(Guid id, UpdatePromotionRequest request);
        Task<bool> DeletePromotionAsync(Guid id);
        Task<PromotionValidationResponse> ValidatePromotionAsync(ValidatePromotionRequest request);
        Task<PromotionUsageDto> ApplyPromotionAsync(ApplyPromotionRequest request);
        Task<List<PromotionUsageDto>> GetUserPromotionHistoryAsync(Guid userId);
        Task<List<PromotionDto>> GetAvailablePromotionsAsync(Guid userId, Guid? houseId);
        Task<PromotionAnalyticsDto> GetPromotionUsageStatsAsync(Guid promotionId);
        Task<List<PromotionAnalyticsDto>> GetPromotionAnalyticsAsync(PromotionSearchParams? searchParams);
    }
}

