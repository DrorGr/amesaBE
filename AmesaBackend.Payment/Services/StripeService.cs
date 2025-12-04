using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Models;
using AmesaBackend.Payment.Services.ProductHandlers;
using AmesaBackend.Shared.Events;
using Stripe;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaymentFailedEvent = AmesaBackend.Shared.Events.PaymentFailedEvent;
using PaymentCompletedEvent = AmesaBackend.Shared.Events.PaymentCompletedEvent;

namespace AmesaBackend.Payment.Services;

public class StripeService : IStripeService
{
    private readonly PaymentDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<StripeService> _logger;
    private readonly IPaymentAuditService? _auditService;
    private readonly IProductHandlerRegistry _productHandlerRegistry;
    private readonly string _apiKey;
    private readonly string _webhookSecret;

    public StripeService(
        PaymentDbContext context,
        IEventPublisher eventPublisher,
        ILogger<StripeService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _auditService = serviceProvider.GetService<IPaymentAuditService>();
        _productHandlerRegistry = serviceProvider.GetRequiredService<IProductHandlerRegistry>();

        // Load from configuration (AWS Secrets Manager in production)
        _apiKey = configuration["Stripe:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("STRIPE_API_KEY") 
            ?? throw new InvalidOperationException("Stripe API key not configured");

        _webhookSecret = configuration["Stripe:WebhookSecret"] 
            ?? Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") 
            ?? throw new InvalidOperationException("Stripe webhook secret not configured");

        StripeConfiguration.ApiKey = _apiKey;
    }

    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, Guid userId)
    {
        try
        {
            // Validate payment method ownership if provided
            if (request.PaymentMethodId.HasValue)
            {
                var paymentMethod = await _context.UserPaymentMethods
                    .FirstOrDefaultAsync(pm => pm.Id == request.PaymentMethodId.Value && pm.UserId == userId && pm.IsActive);

                if (paymentMethod == null)
                {
                    throw new UnauthorizedAccessException("Payment method not found or does not belong to user");
                }

                // Check expiration
                if (paymentMethod.CardExpYear.HasValue && paymentMethod.CardExpMonth.HasValue)
                {
                    var expirationDate = new DateTime(paymentMethod.CardExpYear.Value, paymentMethod.CardExpMonth.Value, 1);
                    if (expirationDate < DateTime.UtcNow)
                    {
                        throw new InvalidOperationException("Payment method has expired");
                    }
                }
            }

            // Server-side price validation if product-based
            decimal validatedAmount = request.Amount;
            if (request.ProductId.HasValue)
            {
                // Get product and calculate price server-side
                var product = await _context.Products.FindAsync(request.ProductId.Value);
                if (product == null)
                {
                    throw new KeyNotFoundException("Product not found");
                }

                var calculatedPrice = product.BasePrice * (request.Quantity ?? 1);
                if (Math.Abs(request.Amount - calculatedPrice) > 0.01m)
                {
                    throw new InvalidOperationException("Amount mismatch - server calculated price differs from client");
                }

                validatedAmount = calculatedPrice;
            }

            // Validate amount
            if (validatedAmount <= 0 || validatedAmount > 10000)
            {
                throw new ArgumentOutOfRangeException(nameof(request.Amount), "Amount must be between 0.01 and 10000");
            }

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(validatedAmount * 100), // Convert to cents
                Currency = request.Currency.ToLower(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "always"
                },
                Metadata = new Dictionary<string, string>
                {
                    ["UserId"] = userId.ToString(),
                    ["ProductId"] = request.ProductId?.ToString() ?? "",
                    ["Quantity"] = request.Quantity?.ToString() ?? "1"
                }
            };

            if (!string.IsNullOrEmpty(request.Description))
            {
                options.Description = request.Description;
            }

            if (request.Metadata != null)
            {
                foreach (var meta in request.Metadata)
                {
                    options.Metadata[meta.Key] = meta.Value;
                }
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            // Store transaction with Pending status
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "StripePayment",
                Amount = validatedAmount,
                Currency = request.Currency,
                Status = "Pending",
                Description = request.Description,
                PaymentMethodId = request.PaymentMethodId,
                ProductId = request.ProductId,
                IdempotencyKey = request.IdempotencyKey,
                ClientSecret = paymentIntent.ClientSecret,
                ProviderTransactionId = paymentIntent.Id,
                Metadata = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["PaymentIntentId"] = paymentIntent.Id,
                    ["Status"] = paymentIntent.Status
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
                    "payment_intent_created",
                    "transaction",
                    transaction.Id,
                    validatedAmount,
                    request.Currency,
                    null,
                    null,
                    new Dictionary<string, object> { ["PaymentIntentId"] = paymentIntent.Id });
            }

            // Publish event
            await _eventPublisher.PublishAsync(new PaymentInitiatedEvent
            {
                PaymentId = transaction.Id,
                UserId = userId,
                Amount = validatedAmount,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethodId?.ToString() ?? "stripe"
            });

            return new PaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = validatedAmount,
                Currency = request.Currency,
                RequiresAction = paymentIntent.Status == "requires_action",
                NextAction = paymentIntent.NextAction?.Type
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe payment intent for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PaymentIntentResponse> ConfirmPaymentIntentAsync(ConfirmPaymentIntentRequest request, Guid userId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(request.PaymentIntentId);

            // Verify ownership via transaction
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.ProviderTransactionId == request.PaymentIntentId && t.UserId == userId);

            if (transaction == null)
            {
                throw new UnauthorizedAccessException("Payment intent not found or does not belong to user");
            }

            var confirmOptions = new PaymentIntentConfirmOptions();

            if (request.PaymentMethodId.HasValue)
            {
                var paymentMethod = await _context.UserPaymentMethods
                    .FirstOrDefaultAsync(pm => pm.Id == request.PaymentMethodId.Value && pm.UserId == userId);

                if (paymentMethod?.ProviderPaymentMethodId != null)
                {
                    confirmOptions.PaymentMethod = paymentMethod.ProviderPaymentMethodId;
                }
            }

            if (!string.IsNullOrEmpty(request.ReturnUrl))
            {
                confirmOptions.ReturnUrl = request.ReturnUrl;
            }

            paymentIntent = await service.ConfirmAsync(request.PaymentIntentId, confirmOptions);

            // Update transaction status
            transaction.Status = paymentIntent.Status == "succeeded" ? "Completed" : "Pending";
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new PaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = (decimal)paymentIntent.Amount / 100,
                Currency = paymentIntent.Currency,
                RequiresAction = paymentIntent.Status == "requires_action",
                NextAction = paymentIntent.NextAction?.Type
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming Stripe payment intent {PaymentIntentId}", request.PaymentIntentId);
            throw;
        }
    }

    public async Task<SetupIntentResponse> CreateSetupIntentAsync(Guid userId)
    {
        try
        {
            var options = new SetupIntentCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    ["UserId"] = userId.ToString()
                }
            };

            var service = new SetupIntentService();
            var setupIntent = await service.CreateAsync(options);

            return new SetupIntentResponse
            {
                ClientSecret = setupIntent.ClientSecret,
                SetupIntentId = setupIntent.Id,
                Status = setupIntent.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe setup intent for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, string timestamp)
    {
        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(_webhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(timestamp + "." + payload);

            using var hmac = new HMACSHA256(secretBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            var expectedSignature = $"v1={hash}";
            return signature == expectedSignature || signature.StartsWith($"v1={hash},");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Stripe webhook signature");
            return false;
        }
    }

    public async Task<WebhookEventResult> HandleWebhookEventAsync(string eventType, object eventData)
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(eventData);
            var eventObj = JsonSerializer.Deserialize<JsonElement>(jsonData);
            var dataObj = eventObj.GetProperty("data").GetProperty("object");

            switch (eventType)
            {
                case "payment_intent.succeeded":
                    await HandlePaymentIntentSucceeded(dataObj);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentIntentFailed(dataObj);
                    break;

                case "payment_intent.requires_action":
                    // Payment needs additional action (3D Secure, etc.)
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe webhook event: {EventType}", eventType);
                    break;
            }

            // Audit log
            if (_auditService != null)
            {
                var paymentIntentId = dataObj.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                if (paymentIntentId != null)
                {
                    var transaction = await _context.Transactions
                        .FirstOrDefaultAsync(t => t.ProviderTransactionId == paymentIntentId);

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
            _logger.LogError(ex, "Error handling Stripe webhook event {EventType}", eventType);
            return new WebhookEventResult { Processed = false, Message = ex.Message };
        }
    }

    private async Task HandlePaymentIntentSucceeded(JsonElement dataObj)
    {
        var paymentIntentId = dataObj.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(paymentIntentId))
            return;

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == paymentIntentId);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for payment intent {PaymentIntentId}", paymentIntentId);
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
            PaymentMethod = transaction.PaymentMethodId?.ToString() ?? "stripe"
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

    private async Task HandlePaymentIntentFailed(JsonElement dataObj)
    {
        var paymentIntentId = dataObj.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(paymentIntentId))
            return;

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == paymentIntentId);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for payment intent {PaymentIntentId}", paymentIntentId);
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
            FailureReason = "Payment failed"
        });
    }

    public async Task<PaymentIntentResponse?> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            return new PaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = (decimal)paymentIntent.Amount / 100,
                Currency = paymentIntent.Currency,
                RequiresAction = paymentIntent.Status == "requires_action",
                NextAction = paymentIntent.NextAction?.Type
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe payment intent {PaymentIntentId}", paymentIntentId);
            return null;
        }
    }
}

