using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Admin.DTOs;
using AmesaBackend.Admin.Security;
using Stripe;
using System.Text.Json;

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
        private readonly IAdminPermissionService _permissions;
        private readonly IAdminAuditService _audit;
        private readonly IConfiguration _configuration;

        public PaymentsService(
            PaymentDbContext context,
            ILogger<PaymentsService> logger,
            IAdminPermissionService permissions,
            IAdminAuditService audit,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _permissions = permissions;
            _audit = audit;
            _configuration = configuration;
        }

        public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(int page = 1, int pageSize = 20, Guid? userId = null, string? status = null, string? type = null)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.PaymentsRead);

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
            await _permissions.RequirePermissionAsync(AdminPermissionNames.PaymentsRead);

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
            await _permissions.RequirePermissionAsync(AdminPermissionNames.PaymentsRefund);

            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null) return false;

            if (transaction.Status != "Completed")
                throw new InvalidOperationException("Only completed transactions can be refunded");

            var refundAmount = amount ?? transaction.Amount;
            if (refundAmount <= 0 || refundAmount > transaction.Amount)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), $"Refund amount must be between 0.01 and {transaction.Amount}");
            }

            var alreadyRefunded = await _context.Transactions
                .Where(t => t.ReferenceId == transaction.Id.ToString() && t.Type == "Refund" && t.Status != "Failed")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            if (alreadyRefunded + refundAmount > transaction.Amount)
            {
                throw new InvalidOperationException("Refund amount exceeds the remaining refundable transaction amount");
            }

            if (string.IsNullOrWhiteSpace(transaction.ProviderTransactionId) ||
                !transaction.ProviderTransactionId.StartsWith("pi_", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only Stripe payment-intent transactions can be refunded by the admin service");
            }

            var providerRefund = await CreateStripeRefundAsync(transaction.ProviderTransactionId, refundAmount, transaction.Currency);

            // Create refund transaction
            var refund = new AmesaBackend.Payment.Models.Transaction
            {
                Id = Guid.NewGuid(),
                UserId = transaction.UserId,
                Type = "Refund",
                Amount = refundAmount,
                Currency = transaction.Currency,
                Status = providerRefund.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase) ? "Completed" : "Pending",
                Description = $"Refund for transaction {transaction.Id}",
                ReferenceId = transaction.Id.ToString(),
                PaymentMethodId = transaction.PaymentMethodId,
                ProviderTransactionId = providerRefund.Id,
                ProductId = transaction.ProductId,
                ProviderResponse = JsonSerializer.Serialize(providerRefund),
                ProcessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(refund);
            transaction.Status = alreadyRefunded + refundAmount == transaction.Amount ? "Refunded" : "PartiallyRefunded";
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction refunded: {TransactionId} - Amount: {Amount}", transactionId, refundAmount);
            await _audit.LogAsync("payment.refund.created", "transaction", transactionId, new
            {
                RefundTransactionId = refund.Id,
                refundAmount,
                transaction.UserId,
                transaction.ProviderTransactionId,
                ProviderRefundId = providerRefund.Id,
                ProviderRefundStatus = providerRefund.Status
            });
            return true;
        }

        private async Task<Refund> CreateStripeRefundAsync(string paymentIntentId, decimal amount, string currency)
        {
            var apiKey = _configuration["Stripe:ApiKey"]
                ?? Environment.GetEnvironmentVariable("STRIPE_API_KEY")
                ?? Environment.GetEnvironmentVariable("Stripe__ApiKey")
                ?? throw new InvalidOperationException("Stripe API key is not configured");

            StripeConfiguration.ApiKey = apiKey;

            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero),
                Metadata = new Dictionary<string, string>
                {
                    ["Source"] = "AmesaAdmin",
                    ["Currency"] = currency
                }
            };

            var service = new RefundService();
            return await service.CreateAsync(options);
        }
    }
}

