using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services.Interfaces
{
    public interface ITicketReservationService
    {
        Task<ReservationDto> CreateReservationAsync(CreateReservationRequest request, Guid houseId, Guid userId);
        Task<ReservationDto?> GetReservationAsync(Guid reservationId, Guid userId);
        Task<bool> CancelReservationAsync(Guid reservationId, Guid userId);
        Task<bool> ValidateReservationAsync(Guid reservationId);
        Task<List<ReservationDto>> GetUserReservationsAsync(Guid userId, string? status = null, int? page = null, int? limit = null);
    }
}



