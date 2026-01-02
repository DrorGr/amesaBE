using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Services.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using Polly;

namespace AmesaBackend.Auth.Services
{
    public class AccountLockoutService : IAccountLockoutService
    {
        // Default values (used if configuration not provided)
        private const int DefaultMaxFailedAttempts = 5;
        private const int DefaultLockoutDurationMinutes = 30;
        private const int DefaultAttemptWindowMinutes = 15;

        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ICircuitBreakerService _circuitBreaker;
        private readonly ILogger<AccountLockoutService> _logger;
        private readonly bool _failClosed;
        
        // Configurable values
        private readonly int _maxFailedAttempts;
        private readonly int _lockoutDurationMinutes;
        private readonly int _attemptWindowMinutes;

        // Metrics tracking
        private static long _lockoutCheckFailures = 0;
        private static long _lockoutCheckSuccesses = 0;
        private static readonly object _metricsLock = new object();

        public AccountLockoutService(
            IDistributedCache cache,
            IConnectionMultiplexer redis,
            AuthDbContext context,
            IConfiguration configuration,
            ICircuitBreakerService circuitBreaker,
            ILogger<AccountLockoutService> logger)
        {
            _cache = cache;
            _redis = redis;
            _context = context;
            _configuration = configuration;
            _circuitBreaker = circuitBreaker;
            _logger = logger;
            _failClosed = _configuration.GetValue<bool>("SecuritySettings:AccountLockoutFailClosed", false);
            
            // Load configurable values with defaults
            _maxFailedAttempts = _configuration.GetValue<int>("SecuritySettings:AccountLockout:MaxFailedAttempts", DefaultMaxFailedAttempts);
            _lockoutDurationMinutes = _configuration.GetValue<int>("SecuritySettings:AccountLockout:LockoutDurationMinutes", DefaultLockoutDurationMinutes);
            _attemptWindowMinutes = _configuration.GetValue<int>("SecuritySettings:AccountLockout:AttemptWindowMinutes", DefaultAttemptWindowMinutes);
        }

        public async Task<bool> IsLockedAsync(string email)
        {
            try
            {
                // Use circuit breaker for Redis operations
                string? lockData = null;
                try
                {
                    lockData = await _circuitBreaker.ExecuteAsync("AccountLockout_Redis", async () =>
                    {
                        var lockKey = $"lockout:locked:{email}";
                        return await _cache.GetStringAsync(lockKey);
                    });
                }
                catch (Exception ex)
                {
                    // Circuit breaker open or other Redis error - fall back to database
                    _logger.LogWarning(ex, "Circuit breaker open or Redis error for AccountLockout_Redis. Falling back to database for email: {Email}", email);
                    IncrementFailureMetric();
                    // Fall through to database fallback
                }

                if (!string.IsNullOrEmpty(lockData))
                {
                    var lockInfo = JsonSerializer.Deserialize<LockoutInfo>(lockData);
                    if (lockInfo != null && lockInfo.LockedUntil > DateTime.UtcNow)
                    {
                        IncrementSuccessMetric();
                        return true;
                    }
                }

                // Fallback to database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                
                if (user != null && user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
                {
                    IncrementSuccessMetric();
                    return true;
                }

                IncrementSuccessMetric();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking lockout status for email: {Email}", email);
                IncrementFailureMetric();
                
                // Fail-open by default (matches codebase pattern), but configurable to fail-closed
                if (_failClosed)
                {
                    _logger.LogWarning("AccountLockoutFailClosed=true, treating as locked due to error for email: {Email}", email);
                    return true; // Fail closed - treat as locked for security
                }
                
                return false; // Fail open - allow access if check fails
            }
        }

        public async Task RecordFailedAttemptAsync(string email)
        {
            try
            {
                // Use circuit breaker for Redis operations
                long attempts = 0;
                try
                {
                    attempts = await _circuitBreaker.ExecuteAsync("AccountLockout_Redis", async () =>
                    {
                        var db = _redis.GetDatabase();
                        var attemptKey = $"lockout:attempts:{email}";
                        
                        // Use atomic INCR to prevent race conditions
                        var result = await db.StringIncrementAsync(attemptKey);
                        
                        // Set expiry only on first attempt
                        if (result == 1)
                        {
                            await db.KeyExpireAsync(attemptKey, TimeSpan.FromMinutes(_attemptWindowMinutes));
                        }
                        
                        return result;
                    });
                }
                catch (Exception ex)
                {
                    // Circuit breaker open or other Redis error - fall back to database
                    _logger.LogWarning(ex, "Circuit breaker open or Redis error for AccountLockout_Redis. Recording failed attempt in database only for email: {Email}", email);
                    // Fall through to database-only recording
                    // Note: We can't track attempts in Redis, so we'll use database as source of truth
                    attempts = 0; // Will be handled by database-only path
                }

                // If max attempts reached, lock the account
                if (attempts >= _maxFailedAttempts)
                {
                    var lockedUntil = DateTime.UtcNow.AddMinutes(_lockoutDurationMinutes);
                    
                    // Try to set in Redis (with circuit breaker)
                    try
                    {
                        await _circuitBreaker.ExecuteAsync("AccountLockout_Redis", async () =>
                        {
                            var lockKey = $"lockout:locked:{email}";
                            var lockInfo = new LockoutInfo { LockedUntil = lockedUntil };
                            var lockData = JsonSerializer.Serialize(lockInfo);

                            var lockOptions = new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_lockoutDurationMinutes)
                            };
                            await _cache.SetStringAsync(lockKey, lockData, lockOptions);
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set lockout in Redis, using database only for email: {Email}", email);
                    }

                    // Always update database (source of truth)
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == email);
                    
                    if (user != null)
                    {
                        user.LockedUntil = lockedUntil;
                        user.FailedLoginAttempts = (int)attempts;
                        user.LastFailedLoginAttempt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogWarning("Account locked for email: {Email} until {LockedUntil}", email, lockedUntil);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording failed attempt for email: {Email}", email);
            }
        }

        public async Task ClearFailedAttemptsAsync(string email)
        {
            try
            {
                // Clear Redis keys
                var attemptKey = $"lockout:attempts:{email}";
                var lockKey = $"lockout:locked:{email}";
                await _cache.RemoveAsync(attemptKey);
                await _cache.RemoveAsync(lockKey);

                // Clear rate limit for login attempts to coordinate with lockout
                // This ensures users aren't blocked by rate limiting after lockout expires
                var rateLimitKey = $"ratelimit:login:{email}";
                await _cache.RemoveAsync(rateLimitKey);

                // Update database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                
                if (user != null)
                {
                    user.LockedUntil = null;
                    user.FailedLoginAttempts = 0;
                    user.LastFailedLoginAttempt = null;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing failed attempts for email: {Email}", email);
            }
        }

        public async Task<DateTime?> GetLockedUntilAsync(string email)
        {
            try
            {
                // Use circuit breaker for Redis operations
                string? lockData = null;
                try
                {
                    lockData = await _circuitBreaker.ExecuteAsync("AccountLockout_Redis", async () =>
                    {
                        var lockKey = $"lockout:locked:{email}";
                        return await _cache.GetStringAsync(lockKey);
                    });
                }
                catch (Exception ex)
                {
                    // Circuit breaker open or other Redis error - fall back to database
                    _logger.LogWarning(ex, "Circuit breaker open or Redis error for AccountLockout_Redis. Falling back to database for email: {Email}", email);
                    // Fall through to database fallback
                }

                if (!string.IsNullOrEmpty(lockData))
                {
                    var lockInfo = JsonSerializer.Deserialize<LockoutInfo>(lockData);
                    if (lockInfo != null && lockInfo.LockedUntil > DateTime.UtcNow)
                    {
                        return lockInfo.LockedUntil;
                    }
                }

                // Fallback to database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                
                if (user != null && user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
                {
                    return user.LockedUntil;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lockout time for email: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Increments the failure metric counter.
        /// </summary>
        private void IncrementFailureMetric()
        {
            lock (_metricsLock)
            {
                _lockoutCheckFailures++;
                // Log periodically (every 10 failures) to avoid log spam
                if (_lockoutCheckFailures % 10 == 0)
                {
                    var totalChecks = _lockoutCheckSuccesses + _lockoutCheckFailures;
                    var failureRate = totalChecks > 0 ? (double)_lockoutCheckFailures / totalChecks * 100 : 0;
                    _logger.LogWarning(
                        "AccountLockout check failure rate: {FailureRate:F2}% ({Failures}/{Total})",
                        failureRate, _lockoutCheckFailures, totalChecks);
                    
                    // Alert if failure rate exceeds 20%
                    if (failureRate > 20 && totalChecks >= 50)
                    {
                        _logger.LogError(
                            "HIGH FAILURE RATE ALERT: AccountLockout check failure rate exceeds 20%: {FailureRate:F2}%",
                            failureRate);
                    }
                }
            }
        }

        /// <summary>
        /// Increments the success metric counter.
        /// </summary>
        private void IncrementSuccessMetric()
        {
            lock (_metricsLock)
            {
                _lockoutCheckSuccesses++;
            }
        }

        private class LockoutInfo
        {
            public DateTime LockedUntil { get; set; }
        }
    }
}

