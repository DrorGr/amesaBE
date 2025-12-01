using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Shared.Events;

namespace AmesaBackend.Payment.Services.ProductHandlers;

public interface IProductHandler
{
    string HandlesType { get; }
    Task<ProductValidationResult> ValidatePurchaseAsync(Guid productId, int quantity, Guid userId, PaymentDbContext context);
    Task<ProcessPurchaseResult> ProcessPurchaseAsync(Guid transactionId, Guid productId, int quantity, Guid userId, PaymentDbContext context, IEventPublisher eventPublisher);
    Task<decimal> CalculatePriceAsync(Guid productId, int quantity, Guid? userId, PaymentDbContext context);
}

public class ProcessPurchaseResult
{
    public bool Success { get; set; }
    public string? LinkedEntityType { get; set; }
    public Guid? LinkedEntityId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
}

