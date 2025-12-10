namespace AmesaBackend.Lottery.Services
{
    public interface IReservationProcessor
    {
        Task<ProcessResult> ProcessReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
    }

    public class ProcessResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}












