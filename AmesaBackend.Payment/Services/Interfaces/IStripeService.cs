using AmesaBackend.Payment.DTOs;

namespace AmesaBackend.Payment.Services.Interfaces;

public interface IStripeService
{
    Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, Guid userId);
    Task<PaymentIntentResponse> ConfirmPaymentIntentAsync(ConfirmPaymentIntentRequest request, Guid userId);
    Task<SetupIntentResponse> CreateSetupIntentAsync(Guid userId);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, string timestamp);
    Task<WebhookEventResult> HandleWebhookEventAsync(string eventType, object eventData);
    Task<PaymentIntentResponse?> GetPaymentIntentAsync(string paymentIntentId);
}

