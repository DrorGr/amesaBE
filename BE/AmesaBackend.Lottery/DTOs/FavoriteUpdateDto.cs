namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Data transfer object representing an update to a user's favorite houses list.
/// Used for real-time notifications to users when houses are added to or removed from their favorites via SignalR.
/// </summary>
public class FavoriteUpdateDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the house that was added to or removed from favorites.
    /// </summary>
    public Guid HouseId { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the house is now in the user's favorites list.
    /// True if added, false if removed.
    /// </summary>
    public bool IsFavorite { get; set; }
    
    /// <summary>
    /// Gets or sets the type of update that occurred.
    /// Valid values: "added" (house was added to favorites) or "removed" (house was removed from favorites).
    /// </summary>
    public string UpdateType { get; set; } = string.Empty; // "added" or "removed"
    
    /// <summary>
    /// Gets or sets the optional display title/name of the house for user-friendly notifications.
    /// </summary>
    public string? HouseTitle { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when this favorite update occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
