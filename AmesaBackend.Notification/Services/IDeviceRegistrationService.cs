using AmesaBackend.Notification.Models;

namespace AmesaBackend.Notification.Services
{
    public interface IDeviceRegistrationService
    {
        Task<DeviceRegistration> RegisterDeviceAsync(Guid userId, string deviceToken, string platform, string? deviceId = null, string? deviceName = null, string? appVersion = null);
        Task<bool> UnregisterDeviceAsync(Guid userId, string deviceToken);
        Task<List<DeviceRegistration>> GetUserDevicesAsync(Guid userId);
        Task<List<string>> GetUserDeviceTokensAsync(Guid userId, string? platform = null);
        Task<bool> UpdateDeviceLastUsedAsync(Guid userId, string deviceToken);
    }
}






