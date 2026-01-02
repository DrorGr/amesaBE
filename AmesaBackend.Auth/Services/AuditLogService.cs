using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services.Interfaces;

namespace AmesaBackend.Auth.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AuthDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            AuthDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogEventAsync(UserAuditLog log)
        {
            try
            {
                // Extract request metadata if not provided
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    if (string.IsNullOrEmpty(log.IpAddress))
                    {
                        log.IpAddress = httpContext.Items["ClientIp"]?.ToString() 
                            ?? httpContext.Connection.RemoteIpAddress?.ToString();
                    }

                    if (string.IsNullOrEmpty(log.UserAgent))
                    {
                        log.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
                    }

                    if (string.IsNullOrEmpty(log.DeviceId))
                    {
                        log.DeviceId = httpContext.Items["DeviceId"]?.ToString();
                    }
                }

                // Save synchronously to ensure data is persisted
                // For high-volume scenarios, consider using a background queue (e.g., Hangfire, Channel)
                try
                {
                    _context.Set<UserAuditLog>().Add(log);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving audit log to database");
                    // Don't throw - audit logging should not break the request
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit event");
                // Don't throw - audit logging should not break the request
            }
        }

        public async Task LogAuthenticationEventAsync(string eventType, Guid? userId, bool success, string? failureReason = null)
        {
            var log = new UserAuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = eventType,
                Success = success,
                FailureReason = failureReason,
                CreatedAt = DateTime.UtcNow
            };

            await LogEventAsync(log);
        }
    }
}

