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

        public PaymentService(PaymentDbContext context, IEventPublisher eventPublisher, ILogger<PaymentService> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
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

        public async Task<TransactionDto> GetTransactionAsync(Guid transactionId)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new KeyNotFoundException("Transaction not found");
            }

            return MapToTransactionDto(transaction);
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(Guid userId, ProcessPaymentRequest request)
        {
            var transaction = new Transaction
            {
                UserId = userId,
                Type = "TicketPurchase", // Default transaction type for ticket purchases
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                Status = "Completed",
                Description = request.Description,
                ReferenceId = request.ReferenceId,
                PaymentMethodId = request.PaymentMethodId,
                ProcessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new PaymentInitiatedEvent
            {
                PaymentId = transaction.Id,
                UserId = userId,
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                PaymentMethod = request.PaymentMethodId.ToString()
            });

            await _eventPublisher.PublishAsync(new PaymentCompletedEvent
            {
                PaymentId = transaction.Id,
                TransactionId = transaction.Id,
                UserId = userId,
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                PaymentMethod = request.PaymentMethodId.ToString()
            });

            return new PaymentResponse
            {
                Success = true,
                TransactionId = transaction.Id.ToString(),
                Message = "Payment processed successfully"
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
