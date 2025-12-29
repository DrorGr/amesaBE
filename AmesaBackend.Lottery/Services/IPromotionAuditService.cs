using AmesaBackend.Lottery.Models;

namespace AmesaBackend.Lottery.Services
{
    public interface IPromotionAuditService
    {
        Task CreateAuditRecordAsync(Guid transactionId, Guid userId, string promotionCode, decimal discountAmount);
        Task<List<PromotionUsageAudit>> GetPendingAuditsAsync();
        Task<List<PromotionUsageAudit>> GetAuditsByTransactionIdAsync(Guid transactionId);
        Task<List<PromotionUsageAudit>> GetAuditsByUserIdAsync(Guid userId);
        Task ResolveAuditAsync(Guid auditId, Guid resolvedByUserId, string resolutionNotes);
        Task<int> GetPendingAuditCountAsync();
    }
}








