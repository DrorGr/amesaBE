using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Admin.DTOs;

namespace AmesaBackend.Admin.Services
{
    public interface IPaymentsService
    {
        Task<PagedResult<TransactionDto>> GetTransactionsAsync(int page = 1, int pageSize = 20, Guid? userId = null, string? status = null, string? type = null);
        Task<TransactionDto?> GetTransactionByIdAsync(Guid id);
        Task<bool> RefundTransactionAsync(Guid transactionId, decimal? amount = null);
    }

    public class PaymentsService : IPaymentsService
    {
        private readonly PaymentDbContext _context;
        private readonly ILogger<PaymentsService> _logger;

        public PaymentsService(
            PaymentDbContext context,
            ILogger<PaymentsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(int page = 1, int pageSize = 20, Guid? userId = null, string? status = null, string? type = null)
        {
            var query = _context.Transactions.AsQueryable();

            if (userId.HasValue)
                query = query.Where(t => t.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status == status);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(t => t.Type == type);

            var totalCount = await query.CountAsync();
            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    Type = t.Type,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Status = t.Status,
                    Description = t.Description,
                    ReferenceId = t.ReferenceId,
                    PaymentMethodId = t.PaymentMethodId,
                    ProviderTransactionId = t.ProviderTransactionId,
                    ProductId = t.ProductId,
                    ProcessedAt = t.ProcessedAt,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<TransactionDto>
            {
                Items = transactions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<TransactionDto?> GetTransactionByIdAsync(Guid id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return null;

            return new TransactionDto
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Status = transaction.Status,
                Description = transaction.Description,
                ReferenceId = transaction.ReferenceId,
                PaymentMethodId = transaction.PaymentMethodId,
                ProviderTransactionId = transaction.ProviderTransactionId,
                ProductId = transaction.ProductId,
                ProcessedAt = transaction.ProcessedAt,
                CreatedAt = transaction.CreatedAt
            };
        }

        public async Task<bool> RefundTransactionAsync(Guid transactionId, decimal? amount = null)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null) return false;

            if (transaction.Status != "Completed")
                throw new InvalidOperationException("Only completed transactions can be refunded");

            var refundAmount = amount ?? transaction.Amount;

            // Create refund transaction
            var refund = new AmesaBackend.Payment.Models.Transaction
            {
                Id = Guid.NewGuid(),
                UserId = transaction.UserId,
                Type = "Refund",
                Amount = refundAmount,
                Currency = transaction.Currency,
                Status = "Pending",
                Description = $"Refund for transaction {transaction.Id}",
                ReferenceId = transaction.Id.ToString(),
                PaymentMethodId = transaction.PaymentMethodId,
                ProductId = transaction.ProductId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(refund);
            transaction.Status = "Refunded";
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction refunded: {TransactionId} - Amount: {Amount}", transactionId, refundAmount);
            return true;
        }
    }
}

