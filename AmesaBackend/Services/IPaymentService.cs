using AmesaBackend.DTOs;

namespace AmesaBackend.Services
{
    public interface IPaymentService
    {
        Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(Guid userId);
        Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, AddPaymentMethodRequest request);
        Task<PaymentMethodDto> UpdatePaymentMethodAsync(Guid userId, Guid paymentMethodId, UpdatePaymentMethodRequest request);
        Task DeletePaymentMethodAsync(Guid userId, Guid paymentMethodId);
        Task<List<TransactionDto>> GetTransactionsAsync(Guid userId);
        Task<TransactionDto> GetTransactionAsync(Guid transactionId);
        Task<PaymentResponse> ProcessPaymentAsync(Guid userId, ProcessPaymentRequest request);
    }
}
