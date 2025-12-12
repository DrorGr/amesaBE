using Microsoft.AspNetCore.SignalR;
using AmesaBackend.Admin.Hubs;

namespace AmesaBackend.Admin.Services
{
    public interface IRealTimeNotificationService
    {
        Task NotifyHouseCreatedAsync(Guid houseId, string title);
        Task NotifyHouseUpdatedAsync(Guid houseId, string title);
        Task NotifyHouseDeletedAsync(Guid houseId);
        Task NotifyUserUpdatedAsync(Guid userId, string email);
        Task NotifyDrawConductedAsync(Guid drawId, Guid? winnerId);
    }

    public class RealTimeNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<AdminHub> _hubContext;
        private readonly ILogger<RealTimeNotificationService> _logger;

        public RealTimeNotificationService(
            IHubContext<AdminHub> hubContext,
            ILogger<RealTimeNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyHouseCreatedAsync(Guid houseId, string title)
        {
            try
            {
                await _hubContext.Clients.Group("houses").SendAsync("HouseCreated", new
                {
                    HouseId = houseId,
                    Title = title,
                    Timestamp = DateTime.UtcNow
                });
                _logger.LogDebug("Notified clients of house creation: {HouseId}", houseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying house creation: {HouseId}", houseId);
            }
        }

        public async Task NotifyHouseUpdatedAsync(Guid houseId, string title)
        {
            try
            {
                await _hubContext.Clients.Group("houses").SendAsync("HouseUpdated", new
                {
                    HouseId = houseId,
                    Title = title,
                    Timestamp = DateTime.UtcNow
                });
                _logger.LogDebug("Notified clients of house update: {HouseId}", houseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying house update: {HouseId}", houseId);
            }
        }

        public async Task NotifyHouseDeletedAsync(Guid houseId)
        {
            try
            {
                await _hubContext.Clients.Group("houses").SendAsync("HouseDeleted", new
                {
                    HouseId = houseId,
                    Timestamp = DateTime.UtcNow
                });
                _logger.LogDebug("Notified clients of house deletion: {HouseId}", houseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying house deletion: {HouseId}", houseId);
            }
        }

        public async Task NotifyUserUpdatedAsync(Guid userId, string email)
        {
            try
            {
                await _hubContext.Clients.Group("users").SendAsync("UserUpdated", new
                {
                    UserId = userId,
                    Email = email,
                    Timestamp = DateTime.UtcNow
                });
                _logger.LogDebug("Notified clients of user update: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user update: {UserId}", userId);
            }
        }

        public async Task NotifyDrawConductedAsync(Guid drawId, Guid? winnerId)
        {
            try
            {
                await _hubContext.Clients.Group("draws").SendAsync("DrawConducted", new
                {
                    DrawId = drawId,
                    WinnerId = winnerId,
                    Timestamp = DateTime.UtcNow
                });
                _logger.LogDebug("Notified clients of draw conducted: {DrawId}", drawId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying draw conducted: {DrawId}", drawId);
            }
        }
    }
}

