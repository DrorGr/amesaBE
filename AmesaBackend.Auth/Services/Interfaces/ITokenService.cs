using AmesaBackend.Auth.Models;

namespace AmesaBackend.Auth.Services;

public interface ITokenService
{
    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(User user, bool rememberMe = false);
    Task<bool> ValidateTokenAsync(string token);
    string GenerateSecureToken();
}

