using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text.Json;

namespace AmesaBackend.Auth.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly IConfiguration _configuration;
        private readonly ICircuitBreakerService _circuitBreaker;
        private readonly ILogger<RateLimitService> _logger;
        private readonly bool _failClosed;

        // Metrics tracking
        private static long _rateLimitCheckFailures = 0;
        private static long _rateLimitCheckSuccesses = 0;
        private static readonly object _metricsLock = new object();

        public RateLimitService(
            IDistributedCache cache,
            IConnectionMultiplexer redis,
            IConfiguration configuration,
            ICircuitBreakerService circuitBreaker,
            ILogger<RateLimitService> logger)
        {
            _cache = cache;
            _redis = redis;
            _configuration = configuration;
            _circuitBreaker = circuitBreaker;
            _logger = logger;
            _failClosed = _configuration.GetValue<bool>("SecuritySettings:RateLimitFailClosed", false);
        }

        public async Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window)
        {
            try
            {
                // Use circuit breaker for Redis operations
                RedisValue countStr = default;
                try
                {
                    countStr = await _circuitBreaker.ExecuteAsync("RateLimit_Redis", async () =>
                    {
                        var db = _redis.GetDatabase();
                        var cacheKey = $"ratelimit:{key}";
                        return await db.StringGetAsync(cacheKey);
                    });
                }
                catch (Exception ex)
                {
                    // Circuit breaker open or other Redis error
                    _logger.LogWarning(ex, "Circuit breaker open or Redis error for RateLimit_Redis. Key: {Key}", key);
                    IncrementFailureMetric();
                    
                    // Fail-open by default (matches codebase pattern), but configurable to fail-closed
                    if (_failClosed)
                    {
                        _logger.LogWarning("RateLimitFailClosed=true, blocking request due to error for key: {Key}", key);
                        return false; // Fail closed - block request if check fails
                    }
                    
                    return true; // Fail open - allow request if cache fails
                }

                if (!countStr.HasValue)
                {
                    IncrementSuccessMetric();
                    return true; // No limit reached
                }

                if (int.TryParse(countStr, out var count))
                {
                    IncrementSuccessMetric();
                    return count < limit;
                }

                IncrementSuccessMetric();
                return true; // Invalid value, allow request
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for key: {Key}", key);
                IncrementFailureMetric();
                
                // Fail-open by default (matches codebase pattern), but configurable to fail-closed
                if (_failClosed)
                {
                    _logger.LogWarning("RateLimitFailClosed=true, blocking request due to error for key: {Key}", key);
                    return false; // Fail closed - block request if check fails
                }
                
                return true; // Fail open - allow request if cache fails
            }
        }

        public async Task IncrementRateLimitAsync(string key, TimeSpan expiry)
        {
            try
            {
                // Use circuit breaker for Redis operations
                await _circuitBreaker.ExecuteAsync("RateLimit_Redis", async () =>
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
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Circuit breaker open or Redis error for RateLimit_Redis. Failed to increment rate limit for key: {Key}", key);
                // Note: We don't fail here - rate limiting is best-effort
                // If Redis is down, we can't track rate limits, but we don't want to block all requests
            }
        }

        public async Task<int> GetCurrentCountAsync(string key)
        {
            try
            {
                // Use circuit breaker for Redis operations
                RedisValue countStr = default;
                try
                {
                    countStr = await _circuitBreaker.ExecuteAsync("RateLimit_Redis", async () =>
                    {
                        var db = _redis.GetDatabase();
                        var cacheKey = $"ratelimit:{key}";
                        return await db.StringGetAsync(cacheKey);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Circuit breaker open or Redis error for RateLimit_Redis. Key: {Key}", key);
                    return 0; // Return 0 if Redis is unavailable
                }

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

        /// <summary>
        /// Increments the failure metric counter.
        /// </summary>
        private void IncrementFailureMetric()
        {
            lock (_metricsLock)
            {
                _rateLimitCheckFailures++;
                // Log periodically (every 10 failures) to avoid log spam
                if (_rateLimitCheckFailures % 10 == 0)
                {
                    var totalChecks = _rateLimitCheckSuccesses + _rateLimitCheckFailures;
                    var failureRate = totalChecks > 0 ? (double)_rateLimitCheckFailures / totalChecks * 100 : 0;
                    _logger.LogWarning(
                        "RateLimit check failure rate: {FailureRate:F2}% ({Failures}/{Total})",
                        failureRate, _rateLimitCheckFailures, totalChecks);
                    
                    // Alert if failure rate exceeds 5%
                    if (failureRate > 5 && totalChecks >= 50)
                    {
                        _logger.LogError(
                            "HIGH FAILURE RATE ALERT: RateLimit check failure rate exceeds 5%: {FailureRate:F2}%",
                            failureRate);
                    }
                }
            }
        }

        /// <summary>
        /// Atomically increments the rate limit counter and checks if the limit is exceeded.
        /// This prevents race conditions where multiple requests pass the check before increment.
        /// Returns true if the request should be allowed (count < limit), false if rate limit exceeded.
        /// </summary>
        public async Task<bool> IncrementAndCheckRateLimitAsync(string key, int limit, TimeSpan window)
        {
            try
            {
                // Use circuit breaker for Redis operations
                long newCount = 0;
                try
                {
                    newCount = await _circuitBreaker.ExecuteAsync("RateLimit_Redis", async () =>
                    {
                        var db = _redis.GetDatabase();
                        var cacheKey = $"ratelimit:{key}";
                        
                        // Use atomic INCR operation to prevent race conditions
                        var result = await db.StringIncrementAsync(cacheKey);
                        
                        // Set expiry only if this is the first increment (result == 1)
                        if (result == 1)
                        {
                            await db.KeyExpireAsync(cacheKey, window);
                        }
                        
                        return result;
                    });
                }
                catch (Exception ex)
                {
                    // Circuit breaker open or other Redis error
                    _logger.LogWarning(ex, "Circuit breaker open or Redis error for RateLimit_Redis. Key: {Key}", key);
                    IncrementFailureMetric();
                    
                    // Fail-open by default (matches codebase pattern), but configurable to fail-closed
                    if (_failClosed)
                    {
                        _logger.LogWarning("RateLimitFailClosed=true, blocking request due to error for key: {Key}", key);
                        return false; // Fail closed - block request if check fails
                    }
                    
                    return true; // Fail open - allow request if cache fails
                }

                // Check if limit exceeded (after increment)
                var isAllowed = newCount <= limit;
                
                if (isAllowed)
                {
                    IncrementSuccessMetric();
                }
                else
                {
                    _logger.LogWarning("Rate limit exceeded for key: {Key}, Count: {Count}, Limit: {Limit}", key, newCount, limit);
                }
                
                return isAllowed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing and checking rate limit for key: {Key}", key);
                IncrementFailureMetric();
                
                // Fail-open by default (matches codebase pattern), but configurable to fail-closed
                if (_failClosed)
                {
                    _logger.LogWarning("RateLimitFailClosed=true, blocking request due to error for key: {Key}", key);
                    return false; // Fail closed - block request if check fails
                }
                
                return true; // Fail open - allow request if cache fails
            }
        }

        /// <summary>
        /// Increments the success metric counter.
        /// </summary>
        private void IncrementSuccessMetric()
        {
            lock (_metricsLock)
            {
                _rateLimitCheckSuccesses++;
            }
        }
    }
}

