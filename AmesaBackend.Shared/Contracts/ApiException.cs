namespace AmesaBackend.Shared.Contracts
{
    /// <summary>
    /// Represents an exception that can be returned as an API error response.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// Gets or sets the HTTP status code associated with this exception.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this exception represents a model validation error.
        /// </summary>
        public bool IsModelValidationError { get; set; }

        /// <summary>
        /// Gets or sets a collection of validation errors when this exception represents a model validation error.
        /// </summary>
        public IEnumerable<ValidationError>? Errors { get; set; }

        /// <summary>
        /// Gets or sets a reference error code for programmatic error handling.
        /// </summary>
        public string? ReferenceErrorCode { get; set; }

        /// <summary>
        /// Gets or sets a link to documentation related to this error.
        /// </summary>
        public string? ReferenceDocumentLink { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with the specified message and optional status code and error details.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="statusCode">The HTTP status code (default: 500).</param>
        /// <param name="errorCode">The reference error code (default: empty string).</param>
        /// <param name="refLink">The reference document link (default: empty string).</param>
        public ApiException(string message,
            int statusCode = 500,
            string errorCode = "",
            string refLink = "") :
            base(message)
        {
            this.IsModelValidationError = false;
            this.StatusCode = statusCode;
            this.ReferenceErrorCode = errorCode;
            this.ReferenceDocumentLink = refLink;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with validation errors.
        /// </summary>
        /// <param name="errors">A collection of validation errors.</param>
        /// <param name="statusCode">The HTTP status code (default: 400).</param>
        public ApiException(IEnumerable<ValidationError> errors, int statusCode = 400)
        {
            this.IsModelValidationError = true;
            this.StatusCode = statusCode;
            this.Errors = errors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class from an existing exception.
        /// </summary>
        /// <param name="ex">The exception to wrap.</param>
        /// <param name="statusCode">The HTTP status code (default: 500).</param>
        public ApiException(System.Exception ex, int statusCode = 500) : base(ex.Message)
        {
            this.IsModelValidationError = false;
            StatusCode = statusCode;
        }
    }
}

