using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Models;
using AmesaBackend.Shared.Events;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaymentFailedEvent = AmesaBackend.Shared.Events.PaymentFailedEvent;
using PaymentCompletedEvent = AmesaBackend.Shared.Events.PaymentCompletedEvent;

namespace AmesaBackend.Payment.Services;

public class CoinbaseCommerceService : ICoinbaseCommerceService
{
    private readonly PaymentDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CoinbaseCommerceService> _logger;
    private readonly IPaymentAuditService? _auditService;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _webhookSecret;
    private const string BASE_URL = "https://api.commerce.coinbase.com";

    public CoinbaseCommerceService(
        PaymentDbContext context,
        IEventPublisher eventPublisher,
        ILogger<CoinbaseCommerceService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _auditService = serviceProvider.GetService<IPaymentAuditService>();
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(BASE_URL);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Load from configuration (AWS Secrets Manager in production)
        _apiKey = configuration["CoinbaseCommerce:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("COINBASE_COMMERCE_API_KEY") 
            ?? throw new InvalidOperationException("Coinbase Commerce API key not configured");

        _webhookSecret = configuration["CoinbaseCommerce:WebhookSecret"] 
            ?? Environment.GetEnvironmentVariable("COINBASE_COMMERCE_WEBHOOK_SECRET") 
            ?? throw new InvalidOperationException("Coinbase Commerce webhook secret not configured");

        _httpClient.DefaultRequestHeaders.Add("X-CC-Api-Key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("X-CC-Version", "2018-03-22");
    }

    public async Task<CoinbaseChargeResponse> CreateChargeAsync(CreateCryptoChargeRequest request, Guid userId)
    {
        try
        {
            // Validate product and calculate price server-side
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive && p.Status == "active" && p.DeletedAt == null);

            if (product == null)
            {
                throw new KeyNotFoundException("Product not found or not available");
            }

            var calculatedPrice = product.BasePrice * request.Quantity;

            // Create charge request
            var chargeRequest = new
            {
                name = product.Name,
                description = product.Description ?? $"Purchase {request.Quantity} x {product.Name}",
                local_price = new
                {
                    amount = calculatedPrice.ToString("F2"),
                    currency = product.Currency
                },
                pricing_type = "fixed_price",
                metadata = new Dictionary<string, string>
                {
                    ["UserId"] = userId.ToString(),
                    ["ProductId"] = product.Id.ToString(),
                    ["Quantity"] = request.Quantity.ToString(),
                    ["IdempotencyKey"] = request.IdempotencyKey ?? Guid.NewGuid().ToString()
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/charges", chargeRequest);
            response.EnsureSuccessStatusCode();

            var chargeData = await response.Content.ReadFromJsonAsync<JsonElement>();
            var charge = chargeData.GetProperty("data");

            var chargeId = charge.GetProperty("id").GetString() ?? "";
            var code = charge.GetProperty("code").GetString() ?? "";
            var hostedUrl = charge.GetProperty("hosted_url").GetString() ?? "";
            var status = charge.GetProperty("status").GetString() ?? "";
            var expiresAt = charge.GetProperty("expires_at").GetDateTime();

            // Parse pricing
            var pricing = charge.GetProperty("pricing");
            var local = pricing.GetProperty("local");
            var localAmount = decimal.Parse(local.GetProperty("amount").GetString() ?? "0");
            var localCurrency = local.GetProperty("currency").GetString() ?? "USD";

            // Parse payments
            var payments = new List<CoinbasePayment>();
            if (charge.TryGetProperty("payments", out var paymentsArray))
            {
                foreach (var payment in paymentsArray.EnumerateArray())
                {
                    payments.Add(new CoinbasePayment
                    {
                        Network = payment.GetProperty("network").GetString() ?? "",
                        TransactionId = payment.GetProperty("transaction_id").GetString() ?? "",
                        Status = payment.GetProperty("status").GetString() ?? "",
                        Value = new CoinbaseValue
                        {
                            Amount = decimal.Parse(payment.GetProperty("value").GetProperty("amount").GetString() ?? "0"),
                            Currency = payment.GetProperty("value").GetProperty("currency").GetString() ?? ""
                        }
                    });
                }
            }

            // Store transaction
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "CryptoPayment",
                Amount = calculatedPrice,
                Currency = product.Currency,
                Status = "Pending",
                Description = $"Crypto payment for {product.Name}",
                ProductId = product.Id,
                IdempotencyKey = request.IdempotencyKey,
                ProviderTransactionId = chargeId,
                Metadata = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["ChargeId"] = chargeId,
                    ["Code"] = code,
                    ["HostedUrl"] = hostedUrl,
                    ["Status"] = status,
                    ["ExpiresAt"] = expiresAt
                }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Audit log
            if (_auditService != null)
            {
                await _auditService.LogActionAsync(
                    userId,
                    "crypto_charge_created",
                    "transaction",
                    transaction.Id,
                    calculatedPrice,
                    product.Currency,
                    null,
                    null,
                    new Dictionary<string, object> { ["ChargeId"] = chargeId });
            }

            // Publish event
            await _eventPublisher.PublishAsync(new PaymentInitiatedEvent
            {
                PaymentId = transaction.Id,
                UserId = userId,
                Amount = calculatedPrice,
                Currency = product.Currency,
                PaymentMethod = "crypto"
            });

            return new CoinbaseChargeResponse
            {
                ChargeId = chargeId,
                Code = code,
                HostedUrl = hostedUrl,
                Status = status,
                ExpiresAt = expiresAt,
                Payments = payments,
                Pricing = new CoinbasePricing
                {
                    Local = new CoinbaseValue
                    {
                        Amount = localAmount,
                        Currency = localCurrency
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Coinbase Commerce charge for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CoinbaseChargeResponse?> GetChargeAsync(string chargeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/charges/{chargeId}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var chargeData = await response.Content.ReadFromJsonAsync<JsonElement>();
            var charge = chargeData.GetProperty("data");

            var code = charge.GetProperty("code").GetString() ?? "";
            var hostedUrl = charge.GetProperty("hosted_url").GetString() ?? "";
            var status = charge.GetProperty("status").GetString() ?? "";
            var expiresAt = charge.GetProperty("expires_at").GetDateTime();

            return new CoinbaseChargeResponse
            {
                ChargeId = chargeId,
                Code = code,
                HostedUrl = hostedUrl,
                Status = status,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Coinbase Commerce charge {ChargeId}", chargeId);
            return null;
        }
    }

    public async Task<bool> VerifyWebhookSignatureAsync(string payload, string signature)
    {
        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(_webhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(secretBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return signature.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Coinbase Commerce webhook signature");
            return false;
        }
    }

    public async Task<WebhookEventResult> HandleWebhookEventAsync(string eventType, object eventData)
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(eventData);
            var eventObj = JsonSerializer.Deserialize<JsonElement>(jsonData);
            var dataObj = eventObj.GetProperty("data");

            switch (eventType)
            {
                case "charge:confirmed":
                    await HandleChargeConfirmed(dataObj);
                    break;

                case "charge:failed":
                    await HandleChargeFailed(dataObj);
                    break;

                default:
                    _logger.LogInformation("Unhandled Coinbase Commerce webhook event: {EventType}", eventType);
                    break;
            }

            // Audit log
            if (_auditService != null)
            {
                var chargeId = dataObj.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                if (chargeId != null)
                {
                    var transaction = await _context.Transactions
                        .FirstOrDefaultAsync(t => t.ProviderTransactionId == chargeId);

                    if (transaction != null)
                    {
                        await _auditService.LogActionAsync(
                            transaction.UserId,
                            $"webhook_{eventType}",
                            "transaction",
                            transaction.Id,
                            transaction.Amount,
                            transaction.Currency,
                            null,
                            null,
                            new Dictionary<string, object> { ["EventType"] = eventType });
                    }
                }
            }

            return new WebhookEventResult { Processed = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Coinbase Commerce webhook event {EventType}", eventType);
            return new WebhookEventResult { Processed = false, Message = ex.Message };
        }
    }

    private async Task HandleChargeConfirmed(JsonElement dataObj)
    {
        var chargeId = dataObj.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(chargeId))
            return;

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == chargeId);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for charge {ChargeId}", chargeId);
            return;
        }

        transaction.Status = "Completed";
        transaction.ProcessedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Publish event
        await _eventPublisher.PublishAsync(new PaymentCompletedEvent
        {
            PaymentId = transaction.Id,
            TransactionId = transaction.Id,
            UserId = transaction.UserId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            PaymentMethod = "crypto"
        });
    }

    private async Task HandleChargeFailed(JsonElement dataObj)
    {
        var chargeId = dataObj.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(chargeId))
            return;

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == chargeId);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for charge {ChargeId}", chargeId);
            return;
        }

        transaction.Status = "Failed";
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Publish event
        await _eventPublisher.PublishAsync(new PaymentFailedEvent
        {
            PaymentId = transaction.Id,
            UserId = transaction.UserId,
            Amount = transaction.Amount,
            FailureReason = "Crypto payment failed"
        });
    }

    public async Task<List<SupportedCrypto>> GetSupportedCryptocurrenciesAsync()
    {
        return new List<SupportedCrypto>
        {
            new SupportedCrypto { Code = "BTC", Name = "Bitcoin", IsStablecoin = false },
            new SupportedCrypto { Code = "ETH", Name = "Ethereum", IsStablecoin = false },
            new SupportedCrypto { Code = "USDC", Name = "USD Coin", IsStablecoin = true },
            new SupportedCrypto { Code = "USDT", Name = "Tether", IsStablecoin = true },
            new SupportedCrypto { Code = "DAI", Name = "Dai", IsStablecoin = true },
            new SupportedCrypto { Code = "BCH", Name = "Bitcoin Cash", IsStablecoin = false },
            new SupportedCrypto { Code = "LTC", Name = "Litecoin", IsStablecoin = false },
            new SupportedCrypto { Code = "DOGE", Name = "Dogecoin", IsStablecoin = false }
        };
    }
}

