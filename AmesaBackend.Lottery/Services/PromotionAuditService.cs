using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services
{
    public class PromotionAuditService : IPromotionAuditService
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<PromotionAuditService> _logger;

        public PromotionAuditService(
            LotteryDbContext context,
            ILogger<PromotionAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateAuditRecordAsync(Guid transactionId, Guid userId, string promotionCode, decimal discountAmount)
        {
            var audit = new PromotionUsageAudit
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                UserId = userId,
                PromotionCode = promotionCode,
                DiscountAmount = discountAmount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.PromotionUsageAudits.Add(audit);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "PromotionUsageAudit created: Transaction {TransactionId}, User {UserId}, Code {PromotionCode}, Amount {DiscountAmount}",
                transactionId, userId, promotionCode, discountAmount);
        }

        public async Task<List<PromotionUsageAudit>> GetPendingAuditsAsync()
        {
            return await _context.PromotionUsageAudits
                .Where(a => a.Status == "Pending")
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PromotionUsageAudit>> GetAuditsByTransactionIdAsync(Guid transactionId)
        {
            return await _context.PromotionUsageAudits
                .Where(a => a.TransactionId == transactionId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PromotionUsageAudit>> GetAuditsByUserIdAsync(Guid userId)
        {
            return await _context.PromotionUsageAudits
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task ResolveAuditAsync(Guid auditId, Guid resolvedByUserId, string resolutionNotes)
        {
            var audit = await _context.PromotionUsageAudits.FindAsync(auditId);
            if (audit == null)
            {
                throw new KeyNotFoundException($"PromotionUsageAudit {auditId} not found");
            }

            if (audit.Status != "Pending")
            {
                throw new InvalidOperationException($"Audit {auditId} is already {audit.Status}");
            }

            audit.Status = "Resolved";
            audit.ResolvedAt = DateTime.UtcNow;
            audit.ResolvedByUserId = resolvedByUserId;
            audit.ResolutionNotes = resolutionNotes;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "PromotionUsageAudit {AuditId} resolved by user {ResolvedByUserId}",
                auditId, resolvedByUserId);
        }

        public async Task<int> GetPendingAuditCountAsync()
        {
            return await _context.PromotionUsageAudits
                .CountAsync(a => a.Status == "Pending");
        }
    }
}





