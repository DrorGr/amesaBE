using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace AmesaBackend.Auth.Services
{
    public class AccountLockoutService : IAccountLockoutService
    {
        private const int MAX_FAILED_ATTEMPTS = 5;
        private const int LOCKOUT_DURATION_MINUTES = 30;
        private const int ATTEMPT_WINDOW_MINUTES = 15;

        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly AuthDbContext _context;
        private readonly ILogger<AccountLockoutService> _logger;

        public AccountLockoutService(
            IDistributedCache cache,
            IConnectionMultiplexer redis,
            AuthDbContext context,
            ILogger<AccountLockoutService> logger)
        {
            _cache = cache;
            _redis = redis;
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsLockedAsync(string email)
        {
            try
            {
                // Check Redis first
                var lockKey = $"lockout:locked:{email}";
                var lockData = await _cache.GetStringAsync(lockKey);
                
                if (!string.IsNullOrEmpty(lockData))
                {
                    var lockInfo = JsonSerializer.Deserialize<LockoutInfo>(lockData);
                    if (lockInfo != null && lockInfo.LockedUntil > DateTime.UtcNow)
                    {
                        return true;
                    }
                }

                // Fallback to database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                
                if (user != null && user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking lockout status for email: {Email}", email);
                return false; // Fail open
            }
        }

        public async Task RecordFailedAttemptAsync(string email)
        {
            try
            {
                var db = _redis.GetDatabase();
                var attemptKey = $"lockout:attempts:{email}";
                
                // Use atomic INCR to prevent race conditions
                var attempts = await db.StringIncrementAsync(attemptKey);
                
                // Set expiry only on first attempt
                if (attempts == 1)
                {
                    await db.KeyExpireAsync(attemptKey, TimeSpan.FromMinutes(ATTEMPT_WINDOW_MINUTES));
                }

                // If max attempts reached, lock the account
                if (attempts >= MAX_FAILED_ATTEMPTS)
                {
                    var lockedUntil = DateTime.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);
                    var lockKey = $"lockout:locked:{email}";
                    var lockInfo = new LockoutInfo { LockedUntil = lockedUntil };
                    var lockData = JsonSerializer.Serialize(lockInfo);

                    var lockOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LOCKOUT_DURATION_MINUTES)
                    };
                    await _cache.SetStringAsync(lockKey, lockData, lockOptions);

                    // Update database
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
                // Check Redis first
                var lockKey = $"lockout:locked:{email}";
                var lockData = await _cache.GetStringAsync(lockKey);
                
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

        private class LockoutInfo
        {
            public DateTime LockedUntil { get; set; }
        }
    }
}

