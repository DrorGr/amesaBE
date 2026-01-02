namespace AmesaBackend.Payment.Services.Interfaces;

public interface IPaymentRateLimitService
{
    Task<bool> CheckPaymentProcessingLimitAsync(Guid userId);
    Task<bool> CheckPaymentMethodCreationLimitAsync(Guid userId);
    Task<bool> CheckTransactionQueryLimitAsync(Guid userId);
    Task IncrementPaymentProcessingAsync(Guid userId);
    Task IncrementPaymentMethodCreationAsync(Guid userId);
    Task IncrementTransactionQueryAsync(Guid userId);
}
