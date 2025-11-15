namespace AmesaBackend.Analytics.Services
{
    public interface IAnalyticsService
    {
        Task<object> GetDashboardAnalyticsAsync(Guid userId);
        Task<object> GetLotteryStatsAsync(Guid? houseId);
        Task LogActivityAsync(Guid? userId, Guid? sessionId, string action, string? resourceType = null, Guid? resourceId = null, object? details = null);
    }
}

