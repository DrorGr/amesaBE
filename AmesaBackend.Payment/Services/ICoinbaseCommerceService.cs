using AmesaBackend.Payment.DTOs;

namespace AmesaBackend.Payment.Services;

public interface ICoinbaseCommerceService
{
    Task<CoinbaseChargeResponse> CreateChargeAsync(CreateCryptoChargeRequest request, Guid userId);
    Task<CoinbaseChargeResponse?> GetChargeAsync(string chargeId);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
    Task<WebhookEventResult> HandleWebhookEventAsync(string eventType, object eventData);
    Task<List<SupportedCrypto>> GetSupportedCryptocurrenciesAsync();
}

