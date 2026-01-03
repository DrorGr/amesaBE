namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Generic paginated response wrapper for API endpoints that return paginated data.
/// Provides pagination metadata along with the actual data items.
/// </summary>
/// <typeparam name="T">The type of items in the paginated response.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the list of items for the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int Limit { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int Total { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of pages available.
    /// Calculated as ceiling(Total / Limit).
    /// </summary>
    public int TotalPages { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether there is a next page available.
    /// </summary>
    public bool HasNext { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether there is a previous page available.
    /// </summary>
    public bool HasPrevious { get; set; }
}
