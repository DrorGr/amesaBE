namespace AmesaBackend.Auth.Services
{
    public interface IRateLimitService
    {
        Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window);
        Task IncrementRateLimitAsync(string key, TimeSpan expiry);
        Task<int> GetCurrentCountAsync(string key);
    }
}

