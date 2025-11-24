using AmesaBackend.Auth.DTOs;

namespace AmesaBackend.Auth.Services
{
    public interface IIdentityVerificationService
    {
        Task<IdentityVerificationResult> VerifyIdentityAsync(Guid userId, VerifyIdentityRequest request);
        Task<IdentityVerificationStatusDto> GetVerificationStatusAsync(Guid userId);
        Task<IdentityVerificationResult> RetryVerificationAsync(Guid userId, VerifyIdentityRequest request);
    }
}

