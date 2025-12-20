using AmesaBackend.Analytics.DTOs;

namespace AmesaBackend.Analytics.Services
{
    public interface IAnalyticsService
    {
        Task<object> GetDashboardAnalyticsAsync(Guid userId, int? page = null, int? limit = null);
        Task<object> GetLotteryStatsAsync(Guid? houseId);
        Task LogActivityAsync(Guid? userId, Guid? sessionId, string action, string? resourceType = null, Guid? resourceId = null, object? details = null);
        Task<List<UserSessionDto>> GetUserSessionsAsync(Guid userId, DateTime? fromDate, DateTime? toDate);
        Task<UserSessionDto?> GetUserSessionAsync(Guid userId, Guid sessionId);
        Task<List<ActivityLogDto>> GetActivityAsync(Guid userId, ActivityFilters filters);
    }
}

