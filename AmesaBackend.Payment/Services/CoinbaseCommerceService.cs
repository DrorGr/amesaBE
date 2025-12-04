using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Models;
using AmesaBackend.Payment.Services.ProductHandlers;
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
    private readonly IProductHandlerRegistry _productHandlerRegistry;
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
        _productHandlerRegistry = serviceProvider.GetRequiredService<IProductHandlerRegistry>();
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

        // Process products via product handlers (CRITICAL: Connect webhook flow to ticket creation)
        await ProcessTransactionProductsAsync(transaction);
    }

    /// <summary>
    /// Processes all products in a completed transaction by calling their respective product handlers.
    /// This connects the webhook flow to ticket creation and other product fulfillment.
    /// </summary>
    private async Task ProcessTransactionProductsAsync(Transaction transaction)
    {
        try
        {
            // Idempotency check: Skip if transaction was already processed
            // This prevents duplicate processing if webhook is called multiple times
            if (transaction.Status == "Completed" && transaction.ProcessedAt.HasValue)
            {
                // Check if products were already processed by checking if this is a retry
                // The product handlers have their own idempotency, but we add defense-in-depth here
                _logger.LogInformation(
                    "Transaction {TransactionId} already marked as completed at {ProcessedAt}. Checking if products need processing.",
                    transaction.Id, transaction.ProcessedAt);
            }

            // Get all transaction items with products
            var transactionItems = await _context.TransactionItems
                .Where(ti => ti.TransactionId == transaction.Id && ti.ProductId.HasValue && ti.ItemType == "product")
                .Include(ti => ti.Product)
                .ToListAsync();

            // Fallback: If no transaction items but transaction has ProductId, use that
            if (!transactionItems.Any() && transaction.ProductId.HasValue)
            {
                _logger.LogInformation(
                    "No transaction items found for transaction {TransactionId}, but ProductId exists. Using fallback logic.",
                    transaction.Id);

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == transaction.ProductId.Value);

                if (product != null)
                {
                    // Extract quantity from metadata if available, default to 1
                    var quantity = 1;
                    if (!string.IsNullOrEmpty(transaction.Metadata))
                    {
                        try
                        {
                            var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(transaction.Metadata);
                            if (metadata != null && metadata.ContainsKey("Quantity"))
                            {
                                if (metadata["Quantity"].ValueKind == JsonValueKind.Number)
                                {
                                    quantity = metadata["Quantity"].GetInt32();
                                }
                                else if (metadata["Quantity"].ValueKind == JsonValueKind.String)
                                {
                                    quantity = int.Parse(metadata["Quantity"].GetString() ?? "1");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse quantity from transaction metadata, using default quantity 1");
                        }
                    }

                    // Create and save transaction item for processing and audit trail
                    var transactionItem = new TransactionItem
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = transaction.Id,
                        ProductId = product.Id,
                        ItemType = "product",
                        Description = product.Name,
                        Quantity = quantity,
                        UnitPrice = quantity > 0 ? transaction.Amount / quantity : transaction.Amount,
                        TotalPrice = transaction.Amount,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.TransactionItems.Add(transactionItem);
                    await _context.SaveChangesAsync();

                    // Load with product for processing
                    transactionItem.Product = product;
                    transactionItems = new List<TransactionItem> { transactionItem };

                    _logger.LogInformation(
                        "Created and saved transaction item for product {ProductId} (quantity: {Quantity}) in transaction {TransactionId}",
                        product.Id, quantity, transaction.Id);
                }
                else
                {
                    _logger.LogWarning(
                        "Product {ProductId} not found for transaction {TransactionId}",
                        transaction.ProductId.Value, transaction.Id);
                }
            }

            if (!transactionItems.Any())
            {
                _logger.LogInformation("No product items found in transaction {TransactionId}", transaction.Id);
                return;
            }

            foreach (var item in transactionItems)
            {
                if (!item.ProductId.HasValue || item.Product == null)
                {
                    _logger.LogWarning("Transaction item {ItemId} has no product", item.Id);
                    continue;
                }

                var product = item.Product;
                var productHandler = _productHandlerRegistry.GetHandler(product.ProductType);

                if (productHandler == null)
                {
                    _logger.LogWarning(
                        "No product handler found for product type '{ProductType}' in transaction {TransactionId}",
                        product.ProductType, transaction.Id);
                    continue;
                }

                try
                {
                    _logger.LogInformation(
                        "Processing product {ProductId} (type: {ProductType}, quantity: {Quantity}) for transaction {TransactionId}",
                        product.Id, product.ProductType, item.Quantity, transaction.Id);

                    var result = await productHandler.ProcessPurchaseAsync(
                        transactionId: transaction.Id,
                        productId: product.Id,
                        quantity: item.Quantity,
                        userId: transaction.UserId,
                        context: _context,
                        eventPublisher: _eventPublisher);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Successfully processed product {ProductId} for transaction {TransactionId}. Linked entity: {LinkedEntityType}/{LinkedEntityId}",
                            product.Id, transaction.Id, result.LinkedEntityType, result.LinkedEntityId);
                    }
                    else
                    {
                        _logger.LogError(
                            "Failed to process product {ProductId} for transaction {TransactionId}: {ErrorMessage}",
                            product.Id, transaction.Id, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing product {ProductId} (type: {ProductType}) for transaction {TransactionId}",
                        product.Id, product.ProductType, transaction.Id);
                    // Continue processing other items even if one fails
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction products for transaction {TransactionId}", transaction.Id);
            // Don't throw - we've already marked transaction as completed and published the event
            // The error is logged for investigation
        }
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

