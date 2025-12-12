namespace AmesaBackend.Admin.Models
{
    public class AdminSession
    {
        public Guid Id { get; set; }
        public Guid AdminUserId { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public AdminUser? AdminUser { get; set; }
    }
}

