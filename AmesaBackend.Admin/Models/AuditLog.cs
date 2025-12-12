namespace AmesaBackend.Admin.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public Guid AdminUserId { get; set; }
        public string? ActionDetails { get; set; } // JSONB
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

