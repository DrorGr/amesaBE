using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services.Processors
{
    public interface ITicketCreatorProcessor
    {
        Task<TicketCreationResult> CreateTicketsAsync(
            Guid reservationId,
            Guid transactionId,
            CancellationToken cancellationToken = default);
    }

    public class TicketCreatorProcessor : ITicketCreatorProcessor
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<TicketCreatorProcessor> _logger;

        public TicketCreatorProcessor(
            LotteryDbContext context,
            ILogger<TicketCreatorProcessor> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TicketCreationResult> CreateTicketsAsync(
            Guid reservationId,
            Guid transactionId,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _context.TicketReservations
                .Include(r => r.House)
                .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

            if (reservation == null)
            {
                return new TicketCreationResult
                {
                    Success = false,
                    ErrorMessage = "Reservation not found"
                };
            }

            if (reservation.Status != "processing")
            {
                return new TicketCreationResult
                {
                    Success = false,
                    ErrorMessage = $"Reservation status is {reservation.Status}, expected processing"
                };
            }

            var tickets = new List<LotteryTicket>();
            var baseTicketNumber = await GetNextTicketNumberAsync(reservation.HouseId, cancellationToken);

            for (int i = 0; i < reservation.Quantity; i++)
            {
                var ticket = new LotteryTicket
                {
                    Id = Guid.NewGuid(),
                    TicketNumber = $"{reservation.HouseId:N}-{baseTicketNumber + i:D6}",
                    HouseId = reservation.HouseId,
                    UserId = reservation.UserId,
                    PurchasePrice = reservation.House.TicketPrice,
                    Status = "Active",
                    PurchaseDate = DateTime.UtcNow,
                    PaymentId = transactionId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                tickets.Add(ticket);
            }

            _context.LotteryTickets.AddRange(tickets);

            reservation.Status = "completed";
            reservation.ProcessedAt = DateTime.UtcNow;
            reservation.PaymentTransactionId = transactionId;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created {Count} tickets for reservation {ReservationId}, transaction {TransactionId}",
                tickets.Count, reservationId, transactionId);

            return new TicketCreationResult
            {
                Success = true,
                TicketIds = tickets.Select(t => t.Id).ToList()
            };
        }

        private async Task<int> GetNextTicketNumberAsync(Guid houseId, CancellationToken cancellationToken)
        {
            var maxTicket = await _context.LotteryTickets
                .Where(t => t.HouseId == houseId)
                .OrderByDescending(t => t.TicketNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (maxTicket == null)
            {
                return 1;
            }

            var parts = maxTicket.TicketNumber.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var number))
            {
                return number + 1;
            }

            return 1;
        }
    }

    public class TicketCreationResult
    {
        public bool Success { get; set; }
        public List<Guid> TicketIds { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
}












