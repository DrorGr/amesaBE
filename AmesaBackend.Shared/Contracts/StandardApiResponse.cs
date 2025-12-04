namespace AmesaBackend.Shared.Contracts
{
    /// <summary>
    /// Standard API response wrapper used across all services
    /// Matches frontend expectations: { success, data, message, error }
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class StandardApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public StandardErrorResponse? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Standard error response details
    /// </summary>
    public class StandardErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Details { get; set; }
    }
}



