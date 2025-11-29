using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using AmesaBackend.Auth.Data;

namespace AmesaBackend.Auth.Middleware
{
    public class EmailVerificationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EmailVerificationMiddleware> _logger;
        private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/v1/auth/verify-email",
            "/api/v1/auth/resend-verification",
            "/api/v1/auth/logout",
            "/api/v1/auth/forgot-password",
            "/api/v1/auth/reset-password",
            "/api/v1/auth/register",
            "/api/v1/auth/login",
            "/api/v1/auth/refresh"
        };
        private const int CACHE_TTL_SECONDS = 300; // 5 minutes

        public EmailVerificationMiddleware(RequestDelegate next, ILogger<EmailVerificationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, AuthDbContext dbContext, IDistributedCache cache)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Skip verification check for allowed paths
            if (AllowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Only check authenticated users
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                await _next(context);
                return;
            }

            try
            {
                // Check cache first to reduce database queries
                var cacheKey = $"email_verified:{userId}";
                var cachedValue = await cache.GetStringAsync(cacheKey);
                
                bool? isVerified = null;
                if (cachedValue != null)
                {
                    isVerified = bool.Parse(cachedValue);
                }
                else
                {
                    // Cache miss - query database
                    var user = await dbContext.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => u.EmailVerified)
                        .FirstOrDefaultAsync();

                    isVerified = user;
                    
                    // Cache the result
                    if (isVerified.HasValue)
                    {
                        await cache.SetStringAsync(cacheKey, isVerified.Value.ToString(), new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CACHE_TTL_SECONDS)
                        });
                    }
                }

                if (isVerified == false)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        error = new
                        {
                            code = "EMAIL_NOT_VERIFIED",
                            message = "Please verify your email before accessing this resource"
                        }
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email verification status");
                // Fail open - allow request if check fails
            }

            await _next(context);
        }
    }
}

