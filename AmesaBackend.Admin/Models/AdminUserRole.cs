namespace AmesaBackend.Admin.Models;

public class AdminUserRole
{
    public Guid AdminUserId { get; set; }
    public Guid AdminRoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedByAdminUserId { get; set; }

    public AdminUser? AdminUser { get; set; }
    public AdminRole? AdminRole { get; set; }
}
