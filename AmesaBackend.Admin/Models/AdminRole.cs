namespace AmesaBackend.Admin.Models;

public class AdminRole
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AdminUserRole> UserRoles { get; set; } = new List<AdminUserRole>();
    public ICollection<AdminRolePermission> RolePermissions { get; set; } = new List<AdminRolePermission>();
}
