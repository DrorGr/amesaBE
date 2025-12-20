using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Analytics.DTOs
{
    public class UserSessionDto
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double DurationSeconds { get; set; }
        public int PageViews { get; set; }
        public int Events { get; set; }
        public string Device { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }

    public class ActivityLogDto
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Page { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }

    public class ActivityFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Limit { get; set; }
    }

    public class PagedResponse<T>
    {
        public bool Success { get; set; } = true;
        public List<T> Items { get; set; } = new();
        public int? Total { get; set; }
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




