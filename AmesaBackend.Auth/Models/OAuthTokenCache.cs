namespace AmesaBackend.Auth.Models
{
    public class OAuthTokenCache
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsNewUser { get; set; }
        public bool UserAlreadyExists { get; set; }
    }
}

