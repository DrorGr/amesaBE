namespace AmesaBackend.Payment.Services.Interfaces;

public interface IPaymentAuditService
{
    Task LogActionAsync(
        Guid userId,
        string action,
        string entityType,
        Guid? entityId,
        decimal? amount,
        string? currency,
        string? ipAddress,
        string? userAgent,
        Dictionary<string, object>? metadata);
}

