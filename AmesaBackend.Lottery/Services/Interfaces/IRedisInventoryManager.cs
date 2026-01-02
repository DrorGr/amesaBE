using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services.Interfaces
{
    public interface IRedisInventoryManager
    {
        Task<bool> ReserveInventoryAsync(Guid houseId, int quantity, string reservationToken);
        Task<bool> ReleaseInventoryAsync(Guid houseId, int quantity);
        Task<InventoryStatus> GetInventoryStatusAsync(Guid houseId);
        Task<int> GetAvailableCountAsync(Guid houseId);
        Task<bool> CheckParticipantCapAsync(Guid houseId, Guid userId);
        Task<bool> AddParticipantAsync(Guid houseId, Guid userId);
    }
}





