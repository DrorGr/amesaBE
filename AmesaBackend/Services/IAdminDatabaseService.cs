namespace AmesaBackend.Services
{
    /// <summary>
    /// Service for managing database connections in admin panel
    /// Displays current environment information (no switching needed)
    /// </summary>
    public interface IAdminDatabaseService
    {
        /// <summary>
        /// Get the current environment (determined by deployment)
        /// </summary>
        string GetCurrentEnvironment();

        /// <summary>
        /// Get the database context for the current environment
        /// </summary>
        Task<AmesaBackend.Data.AmesaDbContext> GetDbContextAsync();
    }
}

