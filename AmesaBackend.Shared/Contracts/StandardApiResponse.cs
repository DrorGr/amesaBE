namespace AmesaBackend.Shared.Contracts
{
    /// <summary>
    /// Standard API response wrapper used across all services
    /// Matches frontend expectations: { success, data, message, error }
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class StandardApiResponse<T>
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the response data.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the error details when the request was not successful.
        /// </summary>
        public StandardErrorResponse? Error { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the response was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Standard error response details
    /// </summary>
    public class StandardErrorResponse
    {
        /// <summary>
        /// Gets or sets the error code for programmatic error handling.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional error details.
        /// </summary>
        public object? Details { get; set; }
    }
}










