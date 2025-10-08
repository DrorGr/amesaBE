using Microsoft.EntityFrameworkCore;
using AmesaBackend.Data;
using AmesaBackend.DTOs;
using AmesaBackend.Models;

namespace AmesaBackend.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(AmesaDbContext context, ILogger<PaymentService> logger)
        {
            _context = context;
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
            // TODO: Implement actual payment method creation with Stripe/PayPal
            var paymentMethod = new UserPaymentMethod
            {
                UserId = userId,
                Type = Enum.TryParse<PaymentMethod>(request.Type, out var type) ? type : PaymentMethod.CreditCard,
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
            // TODO: Implement actual payment processing with Stripe/PayPal
            var transaction = new Transaction
            {
                UserId = userId,
                Type = TransactionType.TicketPurchase,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = PaymentStatus.Completed,
                Description = request.Description,
                ReferenceId = request.ReferenceId,
                PaymentMethodId = request.PaymentMethodId,
                ProcessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

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
                Type = paymentMethod.Type.ToString(),
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
                Type = transaction.Type.ToString(),
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Status = transaction.Status.ToString(),
                Description = transaction.Description,
                ReferenceId = transaction.ReferenceId,
                ProviderTransactionId = transaction.ProviderTransactionId,
                ProcessedAt = transaction.ProcessedAt,
                CreatedAt = transaction.CreatedAt
            };
        }
    }
}
