namespace AmesaBackend.Auth.Services.Interfaces;

/// <summary>
/// Service for syncing notification preferences from Auth service to Notification service
/// </summary>
public interface INotificationPreferencesSyncService
{
    /// <summary>
    /// Syncs notification preferences to Notification service
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="preferencesJson">Preferences JSON containing notification preferences</param>
    /// <returns>True if sync was successful, false otherwise</returns>
    Task<bool> SyncNotificationPreferencesAsync(Guid userId, string preferencesJson);
}
