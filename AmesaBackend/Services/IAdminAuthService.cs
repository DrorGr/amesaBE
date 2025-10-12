namespace AmesaBackend.Services
{
    /// <summary>
    /// Service for admin authentication
    /// </summary>
    public interface IAdminAuthService
    {
        /// <summary>
        /// Authenticate admin user with email and password
        /// </summary>
        Task<bool> AuthenticateAsync(string email, string password);

        /// <summary>
        /// Check if the current user is authenticated as admin
        /// </summary>
        bool IsAuthenticated();

        /// <summary>
        /// Get the current admin user email
        /// </summary>
        string? GetCurrentAdminEmail();

        /// <summary>
        /// Sign out the current admin user
        /// </summary>
        Task SignOutAsync();
    }
}

