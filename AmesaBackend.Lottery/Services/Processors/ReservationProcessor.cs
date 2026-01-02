using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Services.Interfaces;
using AmesaBackend.Lottery.DTOs;
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
        private readonly IPromotionService? _promotionService;
        private readonly ILogger<ReservationProcessor> _logger;

        public ReservationProcessor(
            LotteryDbContext context,
            ITicketReservationService reservationService,
            IPaymentProcessor paymentProcessor,
            ITicketCreatorProcessor ticketCreatorProcessor,
            IRedisInventoryManager inventoryManager,
            ILogger<ReservationProcessor> logger,
            IPromotionService? promotionService = null)
        {
            _context = context;
            _reservationService = reservationService;
            _paymentProcessor = paymentProcessor;
            _ticketCreatorProcessor = ticketCreatorProcessor;
            _inventoryManager = inventoryManager;
            _logger = logger;
            _promotionService = promotionService;
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

            // Apply promotion usage BEFORE payment (fixes race condition)
            // This ensures promotion is locked before payment is processed
            if (!string.IsNullOrWhiteSpace(reservation.PromotionCode) && 
                reservation.DiscountAmount.HasValue && 
                _promotionService != null)
            {
                try
                {
                    // Calculate original price before discount
                    var originalPrice = reservation.TotalPrice + reservation.DiscountAmount.Value;
                    
                    // Use reservation ID as transaction ID - this uniquely identifies the transaction
                    // and will be stored in the reservation for reference
                    await _promotionService.ApplyPromotionAsync(new ApplyPromotionRequest
                    {
                        Code = reservation.PromotionCode,
                        UserId = reservation.UserId,
                        HouseId = reservation.HouseId,
                        Amount = originalPrice,
                        DiscountAmount = reservation.DiscountAmount.Value,
                        TransactionId = reservationId // Use reservation ID as transaction identifier
                    });

                    _logger.LogInformation(
                        "Promotion {PromotionCode} usage recorded for reservation {ReservationId} before payment",
                        reservation.PromotionCode, reservationId);
                }
                catch (Exception promoEx)
                {
                    // If promotion application fails, fail the reservation
                    reservation.Status = "failed";
                    reservation.ErrorMessage = $"Promotion application failed: {promoEx.Message}";
                    reservation.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                    
                    _logger.LogError(promoEx,
                        "Failed to apply promotion {PromotionCode} for reservation {ReservationId}",
                        reservation.PromotionCode, reservationId);
                    
                    return new ProcessResult
                    {
                        Success = false,
                        ErrorMessage = "Promotion application failed"
                    };
                }
            }

            // Idempotency check: If payment already processed, skip payment processing
            PaymentProcessResult paymentResult;
            if (reservation.PaymentTransactionId.HasValue)
            {
                _logger.LogInformation(
                    "Reservation {ReservationId} already has PaymentTransactionId {TransactionId}, skipping payment processing",
                    reservationId, reservation.PaymentTransactionId.Value);
                
                // Use existing transaction ID - payment was already processed
                paymentResult = new PaymentProcessResult
                {
                    Success = true,
                    TransactionId = reservation.PaymentTransactionId.Value
                };
            }
            else
            {
                // Process payment with discounted amount (already calculated and stored in reservation.TotalPrice)
                paymentResult = await _paymentProcessor.ProcessPaymentAsync(
                    reservationId,
                    reservation.PaymentMethodId.Value,
                    reservation.TotalPrice, // This already includes the discount
                    cancellationToken);
            }

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

            // Store PaymentTransactionId for idempotency in a transaction to ensure atomicity
            // This ensures that if payment succeeded, we atomically record it in the database
            // If this fails, we log for manual reconciliation (payment already processed externally)
            if (!reservation.PaymentTransactionId.HasValue)
            {
                using var paymentTrackingTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Re-query reservation within transaction to get latest state
                    var currentReservation = await _context.TicketReservations
                        .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);
                    
                    if (currentReservation == null)
                    {
                        await paymentTrackingTransaction.RollbackAsync(cancellationToken);
                        _logger.LogError(
                            "Reservation {ReservationId} not found when storing PaymentTransactionId. Payment already processed: {TransactionId}. Manual reconciliation required.",
                            reservationId, paymentResult.TransactionId);
                        return new ProcessResult
                        {
                            Success = false,
                            ErrorMessage = "Reservation not found after payment processing"
                        };
                    }

                    // Double-check idempotency within transaction
                    if (!currentReservation.PaymentTransactionId.HasValue)
                    {
                        currentReservation.PaymentTransactionId = paymentResult.TransactionId;
                        currentReservation.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    
                    await paymentTrackingTransaction.CommitAsync(cancellationToken);
                    _logger.LogInformation(
                        "PaymentTransactionId {TransactionId} stored atomically for reservation {ReservationId}",
                        paymentResult.TransactionId, reservationId);
                }
                catch (Exception ex)
                {
                    await paymentTrackingTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex,
                        "CRITICAL: Failed to store PaymentTransactionId {TransactionId} for reservation {ReservationId} after successful payment. " +
                        "Payment was processed but not recorded. Failing operation to trigger retry (payment service idempotency will prevent duplicate charges).",
                        paymentResult.TransactionId, reservationId);
                    
                    // Payment succeeded but we couldn't record it atomically
                    // Fail the operation to trigger retry - payment service idempotency (using reservationId as key)
                    // will return the same transaction ID, allowing us to retry storing PaymentTransactionId
                    // Re-query reservation to ensure it's tracked by EF Core before updating
                    var reservationToUpdate = await _context.TicketReservations
                        .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);
                    
                    if (reservationToUpdate != null)
                    {
                        reservationToUpdate.Status = "failed";
                        reservationToUpdate.ErrorMessage = "Failed to record payment transaction. Operation will be retried.";
                        reservationToUpdate.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        _logger.LogError(
                            "Reservation {ReservationId} not found when updating status after PaymentTransactionId storage failure. Payment already processed: {TransactionId}. Manual reconciliation required.",
                            reservationId, paymentResult.TransactionId);
                    }
                    
                    return new ProcessResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to record payment transaction. Please retry."
                    };
                }
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
                    
                    // Compensate: Refund payment with retry logic
                    var refundSuccess = await RefundPaymentWithRetryAsync(
                        paymentResult.TransactionId,
                        reservation.TotalPrice,
                        reservationId,
                        cancellationToken);
                    
                    reservation.Status = "failed";
                    reservation.ErrorMessage = ticketResult.ErrorMessage;
                    reservation.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    // Only release inventory if refund succeeded
                    // If refund failed, keep inventory reserved for manual reconciliation
                    if (refundSuccess)
                    {
                        await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                        _logger.LogInformation("Refund successful for reservation {ReservationId}, inventory released", reservationId);
                    }
                    else
                    {
                        _logger.LogError(
                            "CRITICAL: Refund failed for reservation {ReservationId}, transaction {TransactionId}. " +
                            "Inventory NOT released - manual reconciliation required. User charged but tickets not created.",
                            reservationId, paymentResult.TransactionId);
                        // Mark reservation for manual review
                        reservation.ErrorMessage = $"{ticketResult.ErrorMessage}. REFUND FAILED - Manual reconciliation required.";
                    }
                    
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
                
                // Compensate: Refund payment with retry logic
                var refundSuccess = await RefundPaymentWithRetryAsync(
                    paymentResult.TransactionId,
                    reservation.TotalPrice,
                    reservationId,
                    cancellationToken);
                
                reservation.Status = "failed";
                reservation.ErrorMessage = $"Database error: {ex.Message}";
                reservation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                
                // Only release inventory if refund succeeded
                if (refundSuccess)
                {
                    await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);
                    _logger.LogInformation("Refund successful for reservation {ReservationId} after exception, inventory released", reservationId);
                }
                else
                {
                    _logger.LogError(
                        "CRITICAL: Refund failed for reservation {ReservationId}, transaction {TransactionId} after exception. " +
                        "Inventory NOT released - manual reconciliation required.",
                        reservationId, paymentResult.TransactionId);
                    reservation.ErrorMessage = $"Database error: {ex.Message}. REFUND FAILED - Manual reconciliation required.";
                }
                
                _logger.LogError(ex, "Error processing reservation {ReservationId}", reservationId);
                
                return new ProcessResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Attempts to refund payment with retry logic
        /// </summary>
        private async Task<bool> RefundPaymentWithRetryAsync(
            Guid transactionId,
            decimal amount,
            Guid reservationId,
            CancellationToken cancellationToken,
            int maxRetries = 3,
            int baseDelayMs = 1000)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var refundSuccess = await _paymentProcessor.RefundPaymentAsync(
                        transactionId,
                        amount,
                        cancellationToken);

                    if (refundSuccess)
                    {
                        _logger.LogInformation(
                            "Refund successful for transaction {TransactionId}, reservation {ReservationId} (attempt {Attempt})",
                            transactionId, reservationId, attempt);
                        return true;
                    }

                    // If refund failed and not last attempt, retry
                    if (attempt < maxRetries)
                    {
                        var delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1); // Exponential backoff
                        _logger.LogWarning(
                            "Refund failed for transaction {TransactionId}, reservation {ReservationId} (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms",
                            transactionId, reservationId, attempt, maxRetries, delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                    }
                    else
                    {
                        _logger.LogError(
                            "Refund failed for transaction {TransactionId}, reservation {ReservationId} after {MaxRetries} attempts",
                            transactionId, reservationId, maxRetries);
                    }
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries)
                    {
                        var delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                        _logger.LogWarning(ex,
                            "Refund exception for transaction {TransactionId}, reservation {ReservationId} (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms",
                            transactionId, reservationId, attempt, maxRetries, delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                    }
                    else
                    {
                        _logger.LogError(ex,
                            "Refund exception for transaction {TransactionId}, reservation {ReservationId} after {MaxRetries} attempts",
                            transactionId, reservationId, maxRetries);
                    }
                }
            }

            return false; // All retry attempts failed
        }
    }
}

