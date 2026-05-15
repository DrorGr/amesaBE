using AmesaBackend.Notification.DTOs;

namespace AmesaBackend.Notification.Services.Interfaces
{
    public interface INotificationOrchestrator
    {
        Task<OrchestrationResult> SendMultiChannelAsync(Guid userId, NotificationRequest request, List<string> channels, Guid? existingNotificationId = null);
        Task<OrchestrationResult> DeliverInAppAsync(Guid userId, NotificationRequest request, Guid? existingNotificationId = null);
        Task<List<DeliveryStatusDto>> GetDeliveryStatusAsync(Guid notificationId);
        Task ResendFailedNotificationAsync(Guid deliveryId);
    }
}












