using AmesaBackend.Notification.DTOs;

namespace AmesaBackend.Notification.Services
{
    public interface IChannelProvider
    {
        string ChannelName { get; }
        Task<DeliveryResult> SendAsync(NotificationRequest request);
        Task<bool> ValidatePreferencesAsync(Guid userId, NotificationPreferences preferences);
        bool IsChannelEnabled(Guid userId);
    }
}












