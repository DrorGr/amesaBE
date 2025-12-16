namespace AmesaBackend.Shared.Contracts
{
    /// <summary>
    /// Represents a generic API response wrapper.
    /// </summary>
    /// <typeparam name="T">The type of data being returned.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets or sets the API version.
        /// </summary>
        public string Version { get; set; } = "1.0.0.0";

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int Code { get; set; } = 200;

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this response represents an error.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Gets or sets the error details when the response represents an error.
        /// </summary>
        public ApiError? ResponseException { get; set; }

        /// <summary>
        /// Gets or sets the response data.
        /// </summary>
        public T? Data { get; set; }
    }
}

