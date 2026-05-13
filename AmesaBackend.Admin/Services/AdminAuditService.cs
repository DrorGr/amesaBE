using System.Text.Json;
using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.Models;

namespace AmesaBackend.Admin.Services;

public interface IAdminAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        Guid entityId,
        object? details = null,
        Guid? adminUserId = null);
}

public sealed class AdminAuditService : IAdminAuditService
{
    private readonly AdminDbContext? _adminDbContext;
    private readonly IAdminPermissionService _permissionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminAuditService> _logger;

    public AdminAuditService(
        IServiceProvider serviceProvider,
        IAdminPermissionService permissionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminAuditService> logger)
    {
        _adminDbContext = serviceProvider.GetService<AdminDbContext>();
        _permissionService = permissionService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid entityId,
        object? details = null,
        Guid? adminUserId = null)
    {
        if (_adminDbContext == null)
        {
            _logger.LogWarning("Admin audit skipped because AdminDbContext is not registered. Action: {Action}, EntityType: {EntityType}, EntityId: {EntityId}", action, entityType, entityId);
            return;
        }

        try
        {
            var resolvedAdminUserId = adminUserId ?? await _permissionService.GetCurrentAdminUserIdAsync() ?? Guid.Empty;
            var enrichedDetails = new
            {
                data = details,
                ipAddress = GetClientIpAddress(),
                userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault(),
                correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier,
                loggedAt = DateTime.UtcNow
            };

            _adminDbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                AdminUserId = resolvedAdminUserId,
                ActionDetails = JsonSerializer.Serialize(enrichedDetails),
                CreatedAt = DateTime.UtcNow
            });

            await _adminDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write admin audit log. Action: {Action}, EntityType: {EntityType}, EntityId: {EntityId}", action, entityType, entityId);
        }
    }

    private string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return "unknown";
        }

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(forwardedFor)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : forwardedFor.Split(',')[0].Trim();
    }
}
