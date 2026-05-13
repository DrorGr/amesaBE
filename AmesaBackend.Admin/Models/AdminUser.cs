namespace AmesaBackend.Admin.Models
{
    public class AdminUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorSecret { get; set; }
        public DateTime? LastMfaAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public ICollection<AdminUserRole> UserRoles { get; set; } = new List<AdminUserRole>();
    }
}

