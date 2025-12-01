using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.DTOs;
using AmesaBackend.Payment.Models;
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

