using AmesaBackend.Auth.DTOs;

namespace AmesaBackend.Auth.Services;

public interface IPasswordResetService
{
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}













