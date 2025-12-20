using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Content.DTOs
{
    public class ContentItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string? ContentBody { get; set; }
        public string? ContentType { get; set; }
        public string? Category { get; set; }
        public string Language { get; set; } = "en";
        public DateTime? PublishedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? FeaturedImageUrl { get; set; }
    }

    public class ContentFilters
    {
        public string? ContentType { get; set; }
        public string? Category { get; set; }
        public string? Language { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedResponse<T>
    {
        public bool Success { get; set; } = true;
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string? Message { get; set; }
        public ErrorResponse? Error { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public ErrorResponse? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Details { get; set; }
    }
}

