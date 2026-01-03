namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Parameters for searching and filtering promotions.
/// Used to query promotions with pagination, filtering, and search capabilities.
/// </summary>
public class PromotionSearchParams
{
    /// <summary>
    /// Gets or sets the page number for pagination (1-based).
    /// Default is 1.
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the number of items per page.
    /// Default is 10.
    /// </summary>
    public int Limit { get; set; } = 10;
    
    /// <summary>
    /// Gets or sets an optional filter for active/inactive promotions.
    /// True for active only, false for inactive only, null for all.
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets an optional filter for promotion type (e.g., "Percentage", "FixedAmount").
    /// </summary>
    public string? Type { get; set; }
    
    /// <summary>
    /// Gets or sets an optional search term to filter promotions by title, code, or description.
    /// </summary>
    public string? Search { get; set; }
    
    /// <summary>
    /// Gets or sets an optional start date filter.
    /// Returns promotions that start on or before this date.
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Gets or sets an optional end date filter.
    /// Returns promotions that end on or after this date.
    /// </summary>
    public DateTime? EndDate { get; set; }
}
