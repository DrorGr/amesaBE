namespace AmesaBackend.Admin.Services;

/// <summary>
/// Interface for admin authentication service
/// </summary>
public interface IAdminAuthService
{
    /// <summary>
    /// Authenticate an admin user with email and password
    /// </summary>
    Task<bool> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Check if the current user is authenticated
    /// </summary>
    bool IsAuthenticated();

    /// <summary>
    /// Get the email of the currently authenticated admin user
    /// </summary>
    string? GetCurrentAdminEmail();

    /// <summary>
    /// Sign out the current admin user
    /// </summary>
    Task SignOutAsync();
}
