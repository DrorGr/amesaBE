namespace AmesaBackend.Auth.Services.Interfaces
{
    public interface IRateLimitService
    {
        Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window);
        Task IncrementRateLimitAsync(string key, TimeSpan expiry);
        Task<int> GetCurrentCountAsync(string key);
        
        /// <summary>
        /// Atomically increments the rate limit counter and checks if the limit is exceeded.
        /// This prevents race conditions where multiple requests pass the check before increment.
        /// Returns true if the request should be allowed (count < limit), false if rate limit exceeded.
        /// </summary>
        Task<bool> IncrementAndCheckRateLimitAsync(string key, int limit, TimeSpan window);
    }
}

