namespace AmesaBackend.Lottery.DTOs;

public class FavoriteUpdateDto
{
    public Guid HouseId { get; set; }
    public bool IsFavorite { get; set; }
    public string UpdateType { get; set; } = string.Empty; // "added" or "removed"
    public string? HouseTitle { get; set; }
    public DateTime Timestamp { get; set; }
}
