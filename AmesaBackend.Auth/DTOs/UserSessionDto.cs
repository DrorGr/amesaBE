namespace AmesaBackend.Auth.DTOs
{
    public class UserSessionDto
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}

