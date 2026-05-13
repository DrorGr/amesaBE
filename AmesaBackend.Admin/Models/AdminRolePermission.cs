namespace AmesaBackend.Admin.Models;

public class AdminRolePermission
{
    public Guid AdminRoleId { get; set; }
    public Guid AdminPermissionId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public AdminRole? AdminRole { get; set; }
    public AdminPermission? AdminPermission { get; set; }
}
