namespace AmesaBackend.Auth.Services.Interfaces
{
    /// <summary>
    /// Service for handling account deletion and closure operations.
    /// </summary>
    public interface IAccountDeletionService
    {
        /// <summary>
        /// Initiates account deletion (soft delete with grace period).
        /// </summary>
        Task<bool> InitiateAccountDeletionAsync(Guid userId, string password);

        /// <summary>
        /// Cancels account deletion if within grace period.
        /// </summary>
        Task<bool> CancelAccountDeletionAsync(Guid userId);

        /// <summary>
        /// Permanently deletes account (after grace period or immediate).
        /// </summary>
        Task<bool> PermanentlyDeleteAccountAsync(Guid userId);

        /// <summary>
        /// Checks if account deletion is pending (within grace period).
        /// </summary>
        Task<bool> IsDeletionPendingAsync(Guid userId);

        /// <summary>
        /// Gets the deletion date (when account will be permanently deleted).
        /// </summary>
        Task<DateTime?> GetDeletionDateAsync(Guid userId);
    }
}



