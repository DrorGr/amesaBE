using AmesaBackend.DTOs;

namespace AmesaBackend.Services
{
    public interface ILotteryService
    {
        Task<List<LotteryTicketDto>> GetUserTicketsAsync(Guid userId);
        Task<LotteryTicketDto> GetTicketAsync(Guid ticketId);
        Task<List<LotteryDrawDto>> GetDrawsAsync();
        Task<LotteryDrawDto> GetDrawAsync(Guid drawId);
        Task ConductDrawAsync(Guid drawId, ConductDrawRequest request);
    }
}
