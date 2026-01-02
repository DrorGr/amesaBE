using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;

namespace AmesaBackend.Auth.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(string refreshToken);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task VerifyEmailAsync(VerifyEmailRequest request);
        Task ResendVerificationEmailAsync(ResendVerificationRequest request);
        Task VerifyPhoneAsync(VerifyPhoneRequest request);
        Task<bool> ValidateTokenAsync(string token);
        Task<UserDto> GetCurrentUserAsync(Guid userId);
        Task<(AuthResponse Response, bool IsNewUser)> CreateOrUpdateOAuthUserAsync(
            string email, 
            string providerId, 
            AuthProvider provider, 
            string? firstName = null, 
            string? lastName = null,
            DateTime? dateOfBirth = null,
            string? gender = null,
            string? profileImageUrl = null);
        Task<List<UserSessionDto>> GetActiveSessionsAsync(Guid userId);
        Task LogoutFromDeviceAsync(Guid userId, string sessionToken);
        Task LogoutAllDevicesAsync(Guid userId);
    }
}

