using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.Models;
using System.Text.Json;

namespace AmesaBackend.Payment.Services;

public class PaymentAuditService : IPaymentAuditService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentAuditService> _logger;

    public PaymentAuditService(PaymentDbContext context, ILogger<PaymentAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(
        Guid userId,
        string action,
        string entityType,
        Guid? entityId,
        decimal? amount,
        string? currency,
        string? ipAddress,
        string? userAgent,
        Dictionary<string, object>? metadata)
    {
        try
        {
            var auditLog = new PaymentAuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Amount = amount,
                Currency = currency,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Fail silently - don't break payment flow if audit logging fails
            _logger.LogError(ex, "Error logging audit action {Action} for user {UserId}", action, userId);
        }
    }
}

