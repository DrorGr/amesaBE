namespace AmesaBackend.Admin.Services;

/// <summary>
/// Service interface for managing admin authentication and authorization.
/// Provides functionality for authenticating admin users, checking authentication status, and managing admin sessions.
/// </summary>
public interface IAdminAuthService
{
    /// <summary>
    /// Authenticates an admin user with email and password credentials.
    /// </summary>
    /// <param name="email">The email address of the admin user.</param>
    /// <param name="password">The password of the admin user.</param>
    /// <returns>True if authentication is successful; otherwise, false.</returns>
    /// <remarks>
    /// This method validates credentials against the admin_users table in the amesa_admin schema.
    /// Passwords are verified using BCrypt hashing.
    /// </remarks>
    Task<bool> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Checks if the current user is authenticated as an admin.
    /// </summary>
    /// <returns>True if the current user is authenticated; otherwise, false.</returns>
    bool IsAuthenticated();

    /// <summary>
    /// Gets the email address of the currently authenticated admin user.
    /// </summary>
    /// <returns>The email address of the authenticated admin user, or null if no user is authenticated.</returns>
    string? GetCurrentAdminEmail();

    /// <summary>
    /// Signs out the current admin user and invalidates their session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SignOutAsync();
}
