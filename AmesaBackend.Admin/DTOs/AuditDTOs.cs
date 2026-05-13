namespace AmesaBackend.Admin.DTOs;

public sealed class AuditLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid AdminUserId { get; set; }
    public string? AdminEmail { get; set; }
    public string? ActionDetails { get; set; }
    public DateTime CreatedAt { get; set; }
}
