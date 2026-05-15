using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.Models;
using AmesaBackend.Admin.Security;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Admin.Services;

public interface IAdminRbacSeedService
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class AdminRbacSeedService : IAdminRbacSeedService
{
    private const string SuperAdminRoleName = "Super Admin";
    private const string OperationsRoleName = "Operations";
    private const string ViewerRoleName = "Viewer";

    private readonly AdminDbContext _db;
    private readonly ILogger<AdminRbacSeedService> _logger;

    public AdminRbacSeedService(AdminDbContext db, ILogger<AdminRbacSeedService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var permissionByName = await EnsurePermissionsAsync(now, cancellationToken);
        var superAdminRole = await EnsureRoleAsync(SuperAdminRoleName, "Full administrative access", true, now, cancellationToken);
        var operationsRole = await EnsureRoleAsync(OperationsRoleName, "Day-to-day operations without admin user management", true, now, cancellationToken);
        var viewerRole = await EnsureRoleAsync(ViewerRoleName, "Read-only access to admin areas", true, now, cancellationToken);

        await EnsureRolePermissionsAsync(superAdminRole.Id, permissionByName.Values.Select(p => p.Id), cancellationToken);
        await EnsureRolePermissionsAsync(operationsRole.Id, GetOperationsPermissionIds(permissionByName), cancellationToken);
        await EnsureRolePermissionsAsync(viewerRole.Id, GetViewerPermissionIds(permissionByName), cancellationToken);
        await AssignSuperAdminToUnassignedUsersAsync(superAdminRole.Id, cancellationToken);

        _logger.LogInformation("Admin RBAC seed completed ({PermissionCount} permissions)", permissionByName.Count);
    }

    private async Task<Dictionary<string, AdminPermission>> EnsurePermissionsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var existing = await _db.AdminPermissions.ToListAsync(cancellationToken);
        var byName = existing.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in AdminPermissionNames.All)
        {
            if (byName.ContainsKey(name))
            {
                continue;
            }

            var permission = new AdminPermission
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = $"Allows {name.Replace('.', ' ')}",
                Category = name.Split('.')[0],
                CreatedAt = now
            };

            _db.AdminPermissions.Add(permission);
            byName[name] = permission;
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return byName;
    }

    private async Task<AdminRole> EnsureRoleAsync(string name, string description, bool isSystemRole, DateTime now, CancellationToken cancellationToken)
    {
        var role = await _db.AdminRoles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        if (role != null)
        {
            return role;
        }

        role = new AdminRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AdminRoles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
        return role;
    }

    private async Task EnsureRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var desired = permissionIds.Distinct().ToHashSet();
        var existing = await _db.AdminRolePermissions
            .Where(rp => rp.AdminRoleId == roleId)
            .Select(rp => rp.AdminPermissionId)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in desired.Except(existing))
        {
            _db.AdminRolePermissions.Add(new AdminRolePermission
            {
                AdminRoleId = roleId,
                AdminPermissionId = permissionId
            });
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static IEnumerable<Guid> GetOperationsPermissionIds(Dictionary<string, AdminPermission> permissionByName)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AdminPermissionNames.AdminUsersManage,
            AdminPermissionNames.SettingsManage
        };

        return permissionByName
            .Where(kvp => !excluded.Contains(kvp.Key))
            .Select(kvp => kvp.Value.Id);
    }

    private static IEnumerable<Guid> GetViewerPermissionIds(Dictionary<string, AdminPermission> permissionByName)
    {
        return permissionByName
            .Where(kvp => kvp.Key.EndsWith(".read", StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value.Id);
    }

    private async Task AssignSuperAdminToUnassignedUsersAsync(Guid superAdminRoleId, CancellationToken cancellationToken)
    {
        var activeAdminIds = await _db.AdminUsers
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (!activeAdminIds.Any())
        {
            return;
        }

        var assignedAdminIds = await _db.AdminUserRoles
            .Select(ur => ur.AdminUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var unassigned = activeAdminIds.Except(assignedAdminIds).ToList();
        foreach (var adminUserId in unassigned)
        {
            _db.AdminUserRoles.Add(new AdminUserRole
            {
                AdminUserId = adminUserId,
                AdminRoleId = superAdminRoleId,
                AssignedAt = DateTime.UtcNow
            });
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Assigned Super Admin role to {Count} admin user(s) without roles", unassigned.Count);
        }
    }
}

public sealed class AdminRbacSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminRbacSeedHostedService> _logger;

    public AdminRbacSeedHostedService(IServiceProvider serviceProvider, ILogger<AdminRbacSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetService<AdminDbContext>();
            if (db == null)
            {
                return;
            }

            var seed = scope.ServiceProvider.GetRequiredService<IAdminRbacSeedService>();
            await seed.SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin RBAC seed failed; bootstrap permission fallback may still apply");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
