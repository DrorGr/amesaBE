using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Models;
using AmesaBackend.Shared.Events;

namespace AmesaBackend.Payment.Services
{
    public class PaymentService : IPaymentService
    {
    private readonly PaymentDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PaymentService> _logger;
    private readonly IPaymentAuditService? _auditService;

    public PaymentService(
        PaymentDbContext context, 
        IEventPublisher eventPublisher, 
        ILogger<PaymentService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _auditService = serviceProvider.GetService<IPaymentAuditService>();
    }

        public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(Guid userId)
        {
            var methods = await _context.UserPaymentMethods
                .Where(pm => pm.UserId == userId && pm.IsActive)
                .OrderBy(pm => pm.IsDefault)
                .ThenBy(pm => pm.CreatedAt)
                .ToListAsync();

            return methods.Select(MapToPaymentMethodDto).ToList();
        }

        public async Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, AddPaymentMethodRequest request)
        {
            var paymentMethod = new UserPaymentMethod
            {
                UserId = userId,
                Type = request.Type,
                Provider = request.Provider,
                CardLastFour = request.CardNumber?.Length > 4 ? request.CardNumber[^4..] : null,
                CardExpMonth = request.ExpMonth,
                CardExpYear = request.ExpYear,
                IsDefault = request.IsDefault,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserPaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            return MapToPaymentMethodDto(paymentMethod);
        }

        public async Task<PaymentMethodDto> UpdatePaymentMethodAsync(Guid userId, Guid paymentMethodId, UpdatePaymentMethodRequest request)
        {
            var paymentMethod = await _context.UserPaymentMethods
                .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.UserId == userId);

            if (paymentMethod == null)
            {
                throw new KeyNotFoundException("Payment method not found");
            }

            if (request.Provider != null)
                paymentMethod.Provider = request.Provider;

            if (request.ExpMonth.HasValue)
                paymentMethod.CardExpMonth = request.ExpMonth.Value;

            if (request.ExpYear.HasValue)
                paymentMethod.CardExpYear = request.ExpYear.Value;

            if (request.IsDefault.HasValue)
                paymentMethod.IsDefault = request.IsDefault.Value;

            paymentMethod.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToPaymentMethodDto(paymentMethod);
        }

        public async Task DeletePaymentMethodAsync(Guid userId, Guid paymentMethodId)
        {
            var paymentMethod = await _context.UserPaymentMethods
                .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.UserId == userId);

            if (paymentMethod == null)
            {
                throw new KeyNotFoundException("Payment method not found");
            }

            paymentMethod.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task<List<TransactionDto>> GetTransactionsAsync(Guid userId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return transactions.Select(MapToTransactionDto).ToList();
        }

        public async Task<TransactionDto> GetTransactionAsync(Guid transactionId, Guid userId)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

            if (transaction == null)
            {
                throw new KeyNotFoundException("Transaction not found");
            }

            return MapToTransactionDto(transaction);
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(Guid userId, ProcessPaymentRequest request, string? ipAddress = null, string? userAgent = null)
        {
            // Check idempotency key
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var existingTransaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey);

                if (existingTransaction != null)
                {
                    return new PaymentResponse
                    {
                        Success = true,
                        TransactionId = existingTransaction.Id.ToString(),
                        ProviderTransactionId = existingTransaction.ProviderTransactionId,
                        Message = "Payment already processed (idempotency)"
                    };
                }
            }

            // Validate payment method ownership
            if (request.PaymentMethodId != Guid.Empty)
            {
                var paymentMethod = await _context.UserPaymentMethods
                    .FirstOrDefaultAsync(pm => pm.Id == request.PaymentMethodId && pm.UserId == userId && pm.IsActive);

                if (paymentMethod == null)
                {
                    throw new UnauthorizedAccessException("Payment method not found or does not belong to user");
                }

                // Check expiration
                if (paymentMethod.CardExpYear.HasValue && paymentMethod.CardExpMonth.HasValue)
                {
                    var expirationDate = new DateTime(paymentMethod.CardExpYear.Value, paymentMethod.CardExpMonth.Value, 1);
                    if (expirationDate < DateTime.UtcNow)
                    {
                        throw new InvalidOperationException("Payment method has expired");
                    }
                }
            }

            // Validate amount
            if (request.Amount <= 0 || request.Amount > 10000)
            {
                throw new ArgumentOutOfRangeException(nameof(request.Amount), "Amount must be between 0.01 and 10000");
            }

            // Server-side price validation if product-based
            decimal validatedAmount = request.Amount;
            if (request.ProductId.HasValue)
            {
                var product = await _context.Products.FindAsync(request.ProductId.Value);
                if (product == null)
                {
                    throw new KeyNotFoundException("Product not found");
                }

                var calculatedPrice = product.BasePrice * (request.Quantity ?? 1);
                if (Math.Abs(request.Amount - calculatedPrice) > 0.01m)
                {
                    throw new InvalidOperationException("Amount mismatch - server calculated price differs from client");
                }

                validatedAmount = calculatedPrice;
            }

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = request.Type ?? "Payment",
                Amount = validatedAmount,
                Currency = request.Currency ?? "USD",
                Status = "Pending", // Changed from "Completed" - payment gateways will update this
                Description = request.Description,
                ReferenceId = request.ReferenceId,
                PaymentMethodId = request.PaymentMethodId != Guid.Empty ? request.PaymentMethodId : null,
                ProductId = request.ProductId,
                IdempotencyKey = request.IdempotencyKey,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new PaymentInitiatedEvent
            {
                PaymentId = transaction.Id,
                UserId = userId,
                Amount = validatedAmount,
                Currency = request.Currency ?? "USD",
                PaymentMethod = request.PaymentMethodId.ToString()
            });

            // Audit log
            if (_auditService != null)
            {
                await _auditService.LogActionAsync(
                    userId,
                    "payment_initiated",
                    "transaction",
                    transaction.Id,
                    validatedAmount,
                    request.Currency ?? "USD",
                    ipAddress,
                    userAgent,
                    new Dictionary<string, object> 
                    { 
                        ["IdempotencyKey"] = request.IdempotencyKey ?? "",
                        ["ProductId"] = request.ProductId?.ToString() ?? "",
                        ["Quantity"] = request.Quantity?.ToString() ?? "1"
                    });
            }

            return new PaymentResponse
            {
                Success = true,
                TransactionId = transaction.Id.ToString(),
                Message = "Payment initiated successfully"
            };
        }

        private PaymentMethodDto MapToPaymentMethodDto(UserPaymentMethod paymentMethod)
        {
            return new PaymentMethodDto
            {
                Id = paymentMethod.Id,
                Type = paymentMethod.Type,
                Provider = paymentMethod.Provider,
                CardLastFour = paymentMethod.CardLastFour,
                CardBrand = paymentMethod.CardBrand,
                CardExpMonth = paymentMethod.CardExpMonth,
                CardExpYear = paymentMethod.CardExpYear,
                IsDefault = paymentMethod.IsDefault,
                IsActive = paymentMethod.IsActive,
                CreatedAt = paymentMethod.CreatedAt
            };
        }

        public async Task<RefundResponse> RefundPaymentAsync(Guid userId, RefundRequest request)
        {
            // Check idempotency key
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var existingRefund = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey && t.Type == "Refund");

                if (existingRefund != null)
                {
                    // Return existing refund transaction
                    var originalTransaction = await _context.Transactions
                        .FirstOrDefaultAsync(t => t.ReferenceId == existingRefund.ReferenceId && t.Type == "Payment");

                    return new RefundResponse
                    {
                        RefundId = existingRefund.Id,
                        TransactionId = originalTransaction?.Id ?? Guid.Empty,
                        RefundAmount = existingRefund.Amount,
                        Status = existingRefund.Status,
                        ProcessedAt = existingRefund.ProcessedAt ?? existingRefund.CreatedAt,
                        ProviderRefundId = existingRefund.ProviderTransactionId,
                        Message = "Refund already processed (idempotency)"
                    };
                }
            }

            // Get original transaction
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == request.TransactionId);

            if (transaction == null)
            {
                throw new KeyNotFoundException("Transaction not found");
            }

            // Authorization: Only transaction owner or admin can refund
            if (transaction.UserId != userId)
            {
                // Check if user is admin (would need to check roles, for now throw)
                throw new UnauthorizedAccessException("You can only refund your own transactions");
            }

            // Validate transaction can be refunded
            if (transaction.Status != "Completed" && transaction.Status != "Pending")
            {
                throw new InvalidOperationException($"Transaction status is {transaction.Status}, cannot refund");
            }

            // Check if already refunded
            var existingRefundTransaction = await _context.Transactions
                .Where(t => t.ReferenceId == transaction.Id.ToString() && t.Type == "Refund" && t.Status == "Completed")
                .FirstOrDefaultAsync();

            if (existingRefundTransaction != null)
            {
                throw new InvalidOperationException("Transaction has already been refunded");
            }

            // Calculate refund amount
            decimal refundAmount = request.PartialAmount ?? transaction.Amount;

            // Validate refund amount
            if (refundAmount <= 0 || refundAmount > transaction.Amount)
            {
                throw new ArgumentOutOfRangeException(nameof(request.PartialAmount), 
                    $"Refund amount must be between 0.01 and {transaction.Amount}");
            }

            // Create refund transaction
            var refundTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "Refund",
                Amount = refundAmount,
                Currency = transaction.Currency,
                Status = "Pending", // Will be updated by payment gateway
                Description = $"Refund for transaction {transaction.Id}" + 
                    (string.IsNullOrEmpty(request.Reason) ? "" : $": {request.Reason}"),
                ReferenceId = transaction.Id.ToString(),
                PaymentMethodId = transaction.PaymentMethodId,
                IdempotencyKey = request.IdempotencyKey,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(refundTransaction);

            // Update original transaction status if full refund
            if (refundAmount == transaction.Amount)
            {
                transaction.Status = "Refunded";
            }
            else
            {
                transaction.Status = "PartiallyRefunded";
            }
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publish refund event
            await _eventPublisher.PublishAsync(new PaymentRefundedEvent
            {
                PaymentId = refundTransaction.Id,
                TransactionId = transaction.Id,
                UserId = userId,
                RefundAmount = refundAmount,
                RefundReason = request.Reason ?? "Refund requested"
            });

            // Audit log
            if (_auditService != null)
            {
                await _auditService.LogActionAsync(
                    userId,
                    "payment_refunded",
                    "transaction",
                    refundTransaction.Id,
                    refundAmount,
                    transaction.Currency,
                    null,
                    null,
                    new Dictionary<string, object>
                    {
                        ["OriginalTransactionId"] = transaction.Id.ToString(),
                        ["Reason"] = request.Reason ?? "",
                        ["IdempotencyKey"] = request.IdempotencyKey ?? ""
                    });
            }

            _logger.LogInformation(
                "Refund created for transaction {TransactionId}, refund amount: {RefundAmount}",
                transaction.Id, refundAmount);

            return new RefundResponse
            {
                RefundId = refundTransaction.Id,
                TransactionId = transaction.Id,
                RefundAmount = refundAmount,
                Status = refundTransaction.Status,
                ProcessedAt = DateTime.UtcNow,
                Message = "Refund initiated successfully"
            };
        }

        public async Task<Guid?> GetTransactionUserIdAsync(Guid transactionId)
        {
            var transaction = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Id == transactionId)
                .Select(t => t.UserId)
                .FirstOrDefaultAsync();

            return transaction;
        }

        private TransactionDto MapToTransactionDto(Transaction transaction)
        {
            return new TransactionDto
            {
                Id = transaction.Id,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Status = transaction.Status,
                Description = transaction.Description,
                ReferenceId = transaction.ReferenceId,
                ProviderTransactionId = transaction.ProviderTransactionId,
                ProcessedAt = transaction.ProcessedAt,
                CreatedAt = transaction.CreatedAt
            };
        }
    }
}
