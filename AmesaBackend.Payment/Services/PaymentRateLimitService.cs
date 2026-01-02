using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Payment.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Payment.Services;

public class PaymentRateLimitService : IPaymentRateLimitService
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<PaymentRateLimitService> _logger;

    // Rate limits
    private const int PAYMENT_PROCESSING_LIMIT = 10; // 10 payments per hour
    private const int PAYMENT_METHOD_CREATION_LIMIT = 5; // 5 payment methods per hour
    private const int TRANSACTION_QUERY_LIMIT = 100; // 100 queries per hour

    public PaymentRateLimitService(
        IRateLimitService rateLimitService,
        ILogger<PaymentRateLimitService> logger)
    {
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task<bool> CheckPaymentProcessingLimitAsync(Guid userId)
    {
        var key = $"payment:process:{userId}";
        var limit = await _rateLimitService.CheckRateLimitAsync(key, PAYMENT_PROCESSING_LIMIT, TimeSpan.FromHours(1));
        
        if (!limit)
        {
            _logger.LogWarning("Payment processing rate limit exceeded for user {UserId}", userId);
        }
        
        return limit;
    }

    public async Task IncrementPaymentProcessingAsync(Guid userId)
    {
        var key = $"payment:process:{userId}";
        await _rateLimitService.IncrementRateLimitAsync(key, TimeSpan.FromHours(1));
    }

    public async Task<bool> CheckPaymentMethodCreationLimitAsync(Guid userId)
    {
        var key = $"payment:method:create:{userId}";
        var limit = await _rateLimitService.CheckRateLimitAsync(key, PAYMENT_METHOD_CREATION_LIMIT, TimeSpan.FromHours(1));
        
        if (!limit)
        {
            _logger.LogWarning("Payment method creation rate limit exceeded for user {UserId}", userId);
        }
        
        return limit;
    }

    public async Task IncrementPaymentMethodCreationAsync(Guid userId)
    {
        var key = $"payment:method:create:{userId}";
        await _rateLimitService.IncrementRateLimitAsync(key, TimeSpan.FromHours(1));
    }

    public async Task<bool> CheckTransactionQueryLimitAsync(Guid userId)
    {
        var key = $"payment:query:{userId}";
        var limit = await _rateLimitService.CheckRateLimitAsync(key, TRANSACTION_QUERY_LIMIT, TimeSpan.FromHours(1));
        
        if (!limit)
        {
            _logger.LogWarning("Transaction query rate limit exceeded for user {UserId}", userId);
        }
        
        return limit;
    }

    public async Task IncrementTransactionQueryAsync(Guid userId)
    {
        var key = $"payment:query:{userId}";
        await _rateLimitService.IncrementRateLimitAsync(key, TimeSpan.FromHours(1));
    }
}

