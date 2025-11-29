using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace AmesaBackend.Auth.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RateLimitService> _logger;

        public RateLimitService(
            IDistributedCache cache,
            IConnectionMultiplexer redis,
            ILogger<RateLimitService> logger)
        {
            _cache = cache;
            _redis = redis;
            _logger = logger;
        }

        public async Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window)
        {
            try
            {
                var db = _redis.GetDatabase();
                var cacheKey = $"ratelimit:{key}";
                
                // Use atomic GET to check current count
                var countStr = await db.StringGetAsync(cacheKey);
                
                if (!countStr.HasValue)
                {
                    return true; // No limit reached
                }

                if (int.TryParse(countStr, out var count))
                {
                    return count < limit;
                }

                return true; // Invalid value, allow request
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for key: {Key}", key);
                return true; // Fail open - allow request if cache fails
            }
        }

        public async Task IncrementRateLimitAsync(string key, TimeSpan expiry)
        {
            try
            {
                var db = _redis.GetDatabase();
                var cacheKey = $"ratelimit:{key}";
                
                // Use atomic INCR operation to prevent race conditions
                var newCount = await db.StringIncrementAsync(cacheKey);
                
                // Set expiry only if this is the first increment (newCount == 1)
                if (newCount == 1)
                {
                    await db.KeyExpireAsync(cacheKey, expiry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing rate limit for key: {Key}", key);
            }
        }

        public async Task<int> GetCurrentCountAsync(string key)
        {
            try
            {
                var db = _redis.GetDatabase();
                var cacheKey = $"ratelimit:{key}";
                var countStr = await db.StringGetAsync(cacheKey);
                
                if (countStr.HasValue && int.TryParse(countStr, out var count))
                {
                    return count;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rate limit count for key: {Key}", key);
                return 0;
            }
        }
    }
}

