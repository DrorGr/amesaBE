namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for checking if passwords have been compromised in data breaches using Have I Been Pwned API.
    /// </summary>
    public interface IPasswordBreachService
    {
        /// <summary>
        /// Checks if a password has been found in data breaches.
        /// Uses k-anonymity model: only sends first 5 characters of SHA-1 hash to HIBP API.
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <returns>True if password was found in breaches, false otherwise</returns>
        Task<bool> IsPasswordBreachedAsync(string password);

        /// <summary>
        /// Gets the number of times a password was found in breaches (if available).
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <returns>Number of breaches, or 0 if not found</returns>
        Task<int> GetBreachCountAsync(string password);
    }
}



