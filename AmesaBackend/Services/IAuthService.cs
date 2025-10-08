using AmesaBackend.DTOs;

namespace AmesaBackend.Services
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
        Task VerifyPhoneAsync(VerifyPhoneRequest request);
        Task<bool> ValidateTokenAsync(string token);
        Task<UserDto> GetCurrentUserAsync(Guid userId);
    }
}
