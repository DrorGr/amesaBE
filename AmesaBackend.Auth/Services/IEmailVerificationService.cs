using AmesaBackend.Auth.DTOs;

namespace AmesaBackend.Auth.Services;

public interface IEmailVerificationService
{
    Task VerifyEmailAsync(VerifyEmailRequest request);
    Task ResendVerificationEmailAsync(ResendVerificationRequest request);
}













