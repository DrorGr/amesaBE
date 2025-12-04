using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services.Processors
{
    public class ReservationProcessor : IReservationProcessor
    {
        private readonly LotteryDbContext _context;
        private readonly ITicketReservationService _reservationService;
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly ITicketCreatorProcessor _ticketCreatorProcessor;
        private readonly IRedisInventoryManager _inventoryManager;
        private readonly ILogger<ReservationProcessor> _logger;

        public ReservationProcessor(
            LotteryDbContext context,
            ITicketReservationService reservationService,
            IPaymentProcessor paymentProcessor,
            ITicketCreatorProcessor ticketCreatorProcessor,
            IRedisInventoryManager inventoryManager,
            ILogger<ReservationProcessor> logger)
        {
            _context = context;
            _reservationService = reservationService;
            _paymentProcessor = paymentProcessor;
            _ticketCreatorProcessor = ticketCreatorProcessor;
            _inventoryManager = inventoryManager;
            _logger = logger;
        }

        public async Task<ProcessResult> ProcessReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await _context.TicketReservations
                .Include(r => r.House)
                .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

            if (reservation == null)
            {
                return new ProcessResult
                {
                    Success = false,
                    ErrorMessage = "Reservation not found"
                };
            }

            // Check if expired
            if (reservation.ExpiresAt <= DateTime.UtcNow)
            {
                reservation.Status = "expired";
                reservation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                
                return new ProcessResult
                {
                    Success = false,
                    ErrorMessage = "Reservation expired"
                };
            }

            // Check if already processed
            if (reservation.Status != "pending")
            {
                return new ProcessResult
                {
                    Success = false,
                    ErrorMessage = $"Reservation already {reservation.Status}"
                };
            }

            // Validate reservation
            var isValid = await _reservationService.ValidateReservationAsync(reservationId);
            if (!isValid)
            {
                reservation.Status = "expired";
                reservation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                
                return new ProcessResult
                {
                    Success = false,
                    ErrorMessage = "Reservation validation failed"
                };
            }

            // Update status to processing
            reservation.Status = "processing";
            reservation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Process payment
            if (!reservation.PaymentMethodId.HasValue)
            {
                reservation.Status = "failed";
                reservation.ErrorMessage = "Payment method not specified";
                reservation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                
                return new ProcessResult
                {
                    Success = false,
                    ErrorMessage = "Payment method not specified"
                };
            }

            var paymentResult = await _paymentProcessor.ProcessPaymentAsync(
                reservationId,
                reservation.PaymentMethodId.Value,
                reservation.TotalPrice,
                cancellationToken);

            if (!paymentResult.Success)
            {
                reservation.Status = "failed";
                reservation.ErrorMessage = paymentResult.ErrorMessage;
                reservation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                
                return new ProcessResult
                {
                    Success = false,
                    ErrorMessage = paymentResult.ErrorMessage ?? "Payment failed"
                };
            }

            // Create tickets in database transaction
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var ticketResult = await _ticketCreatorProcessor.CreateTicketsAsync(
                    reservationId,
                    paymentResult.TransactionId,
                    cancellationToken);

                if (!ticketResult.Success)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    
                    // Compensate: Refund payment
                    await _paymentProcessor.RefundPaymentAsync(
                        paymentResult.TransactionId,
                        reservation.TotalPrice,
                        cancellationToken);
                    
                    reservation.Status = "failed";
                    reservation.ErrorMessage = ticketResult.ErrorMessage;
                    reservation.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                    
                    return new ProcessResult
                    {
                        Success = false,
                        ErrorMessage = ticketResult.ErrorMessage ?? "Failed to create tickets"
                    };
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Reservation {ReservationId} processed successfully", reservationId);
                
                return new ProcessResult { Success = true };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                
                // Compensate: Refund payment
                await _paymentProcessor.RefundPaymentAsync(
                    paymentResult.TransactionId,
                    reservation.TotalPrice,
                    cancellationToken);
                
                reservation.Status = "failed";
                reservation.ErrorMessage = $"Database error: {ex.Message}";
                reservation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                
                await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                
                _logger.LogError(ex, "Error processing reservation {ReservationId}", reservationId);
                
                return new ProcessResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}

