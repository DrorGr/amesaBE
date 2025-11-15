namespace AmesaBackend.Auth.Services
{
    public interface IAdminAuthService
    {
        Task<bool> AuthenticateAsync(string email, string password);
        bool IsAuthenticated();
        string? GetCurrentAdminEmail();
        Task SignOutAsync();
    }
}

