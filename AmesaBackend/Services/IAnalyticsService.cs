namespace AmesaBackend.Services
{
    public interface IAnalyticsService
    {
        Task<object> GetDashboardAnalyticsAsync(Guid userId);
        Task<object> GetLotteryStatsAsync(Guid? houseId);
    }
}
