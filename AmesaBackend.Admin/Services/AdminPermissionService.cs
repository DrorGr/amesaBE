using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.Security;
using AmesaBackend.Auth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Admin.Services;

public interface IAdminPermissionService
{
    Task<Guid?> GetCurrentAdminUserIdAsync();
    Task<IReadOnlyCollection<string>> GetCurrentPermissionsAsync();
    Task<IReadOnlyCollection<string>> GetPermissionsForAdminAsync(string adminEmail);
    Task<IReadOnlyCollection<string>> GetRolesForAdminAsync(string adminEmail);
    Task<bool> HasPermissionAsync(string permission);
    Task RequirePermissionAsync(string permission);
}

public sealed class AdminPermissionService : IAdminPermissionService
{
    private readonly AdminDbContext? _adminDbContext;
    private readonly IAdminAuthService _authService;
    private readonly ILogger<AdminPermissionService> _logger;

    public AdminPermissionService(
        IServiceProvider serviceProvider,
        IAdminAuthService authService,
        ILogger<AdminPermissionService> logger)
    {
        _adminDbContext = serviceProvider.GetService<AdminDbContext>();
        _authService = authService;
        _logger = logger;
    }

    public async Task<Guid?> GetCurrentAdminUserIdAsync()
    {
        var email = _authService.GetCurrentAdminEmail();
        if (string.IsNullOrWhiteSpace(email) || _adminDbContext == null)
        {
            return null;
        }

        try
        {
            return await _adminDbContext.AdminUsers
                .AsNoTracking()
                .Where(u => EF.Functions.ILike(u.Email, email.Trim()) && u.IsActive)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve current admin user id");
            return null;
        }
    }

    public async Task<IReadOnlyCollection<string>> GetCurrentPermissionsAsync()
    {
        var email = _authService.GetCurrentAdminEmail();
        return string.IsNullOrWhiteSpace(email)
            ? Array.Empty<string>()
            : await GetPermissionsForAdminAsync(email);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsForAdminAsync(string adminEmail)
    {
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            return Array.Empty<string>();
        }

        if (_adminDbContext == null)
        {
            _logger.LogWarning("Admin RBAC tables unavailable because AdminDbContext is not registered. Falling back to full permissions for authenticated admin.");
            return AdminPermissionNames.All;
        }

        try
        {
            var normalizedEmail = adminEmail.Trim();
            var permissions = await _adminDbContext.AdminUsers
                .AsNoTracking()
                .Where(u => EF.Functions.ILike(u.Email, normalizedEmail) && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.AdminRole!.RolePermissions)
                .Select(rp => rp.AdminPermission!.Name)
                .Distinct()
                .ToListAsync();

            if (permissions.Count > 0)
            {
                return permissions;
            }

            if (await ShouldUseBootstrapSuperAdminFallbackAsync(normalizedEmail))
            {
                return AdminPermissionNames.All;
            }

            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate admin permissions. Falling back to full permissions for authenticated admin until RBAC schema is seeded.");
            return AdminPermissionNames.All;
        }
    }

    public async Task<IReadOnlyCollection<string>> GetRolesForAdminAsync(string adminEmail)
    {
        if (string.IsNullOrWhiteSpace(adminEmail) || _adminDbContext == null)
        {
            return new[] { "Admin" };
        }

        try
        {
            var roles = await _adminDbContext.AdminUsers
                .AsNoTracking()
                .Where(u => EF.Functions.ILike(u.Email, adminEmail.Trim()) && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .Select(ur => ur.AdminRole!.Name)
                .Distinct()
                .ToListAsync();

            return roles.Count > 0 ? roles : new[] { "Admin" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate admin roles");
            return new[] { "Admin" };
        }
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        var permissions = await GetCurrentPermissionsAsync();
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public async Task RequirePermissionAsync(string permission)
    {
        if (!await HasPermissionAsync(permission))
        {
            throw new UnauthorizedAccessException($"Admin permission required: {permission}");
        }
    }

    private async Task<bool> ShouldUseBootstrapSuperAdminFallbackAsync(string normalizedEmail)
    {
        try
        {
            var activeAdminExists = await _adminDbContext!.AdminUsers
                .AsNoTracking()
                .AnyAsync(u => EF.Functions.ILike(u.Email, normalizedEmail) && u.IsActive);

            if (!activeAdminExists)
            {
                return false;
            }

            var anyRoleAssignments = await _adminDbContext.AdminUserRoles.AnyAsync();
            if (!anyRoleAssignments)
            {
                _logger.LogWarning("No admin role assignments exist. Granting bootstrap full permissions to active admin {AdminEmail}. Seed RBAC roles before disabling bootstrap fallback.", normalizedEmail);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate bootstrap admin permission fallback");
            return true;
        }

        return false;
    }
}
