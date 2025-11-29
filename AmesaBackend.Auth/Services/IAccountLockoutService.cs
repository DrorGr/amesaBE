namespace AmesaBackend.Auth.Services
{
    public interface IAccountLockoutService
    {
        Task<bool> IsLockedAsync(string email);
        Task RecordFailedAttemptAsync(string email);
        Task ClearFailedAttemptsAsync(string email);
        Task<DateTime?> GetLockedUntilAsync(string email);
    }
}

