using AmesaBackend.Auth.Models;

namespace AmesaBackend.Auth.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogEventAsync(UserAuditLog log);
        Task LogAuthenticationEventAsync(string eventType, Guid? userId, bool success, string? failureReason = null);
    }
}

