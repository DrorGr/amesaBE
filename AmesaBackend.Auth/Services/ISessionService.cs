using AmesaBackend.Auth.DTOs;

namespace AmesaBackend.Auth.Services;

public interface ISessionService
{
    Task EnforceSessionLimitAsync(Guid userId);
    Task<List<UserSessionDto>> GetActiveSessionsAsync(Guid userId);
    Task LogoutFromDeviceAsync(Guid userId, string sessionToken);
    Task LogoutAllDevicesAsync(Guid userId);
    Task InvalidateAllSessionsAsync(Guid userId);
    string GenerateDeviceId(string userAgent, string ipAddress);
    string ExtractDeviceName(string userAgent);
}









