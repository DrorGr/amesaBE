using AmesaBackend.Auth.DTOs;

namespace AmesaBackend.Auth.Services.Interfaces
{
    public interface IPasswordValidatorService
    {
        Task<PasswordValidationResult> ValidatePasswordAsync(string password, Guid? userId = null);
        Task<bool> IsPasswordInHistoryAsync(string password, Guid userId);
    }
}

