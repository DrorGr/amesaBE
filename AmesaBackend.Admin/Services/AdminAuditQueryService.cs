using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.DTOs;
using AmesaBackend.Admin.Security;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Admin.Services;

public interface IAdminAuditQueryService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 50,
        string? action = null,
        string? entityType = null,
        Guid? adminUserId = null);
}

public sealed class AdminAuditQueryService : IAdminAuditQueryService
{
    private readonly AdminDbContext? _adminDbContext;
    private readonly IAdminPermissionService _permissions;

    public AdminAuditQueryService(
        IServiceProvider serviceProvider,
        IAdminPermissionService permissions)
    {
        _adminDbContext = serviceProvider.GetService<AdminDbContext>();
        _permissions = permissions;
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 50,
        string? action = null,
        string? entityType = null,
        Guid? adminUserId = null)
    {
        await _permissions.RequirePermissionAsync(AdminPermissionNames.AuditRead);

        if (_adminDbContext == null)
        {
            return new PagedResult<AuditLogDto>();
        }

        var query = _adminDbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => EF.Functions.ILike(a.Action, $"%{action.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => EF.Functions.ILike(a.EntityType, entityType.Trim()));
        }

        if (adminUserId.HasValue)
        {
            query = query.Where(a => a.AdminUserId == adminUserId.Value);
        }

        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                AdminUserId = a.AdminUserId,
                AdminEmail = _adminDbContext.AdminUsers
                    .Where(u => u.Id == a.AdminUserId)
                    .Select(u => u.Email)
                    .FirstOrDefault(),
                ActionDetails = a.ActionDetails,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<AuditLogDto>
        {
            Items = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
