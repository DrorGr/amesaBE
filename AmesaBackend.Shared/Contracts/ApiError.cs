using System.Text.Json.Serialization;

namespace AmesaBackend.Shared.Contracts
{
    /// <summary>
    /// Represents an error response returned by the API.
    /// </summary>
    public class ApiError
    {
        /// <summary>
        /// Gets or sets the exception message describing the error.
        /// </summary>
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets additional details about the error.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets a reference error code for programmatic error handling.
        /// </summary>
        public string? ReferenceErrorCode { get; set; }

        /// <summary>
        /// Gets or sets a link to documentation related to this error.
        /// </summary>
        public string? ReferenceDocumentLink { get; set; }

        /// <summary>
        /// Gets or sets a collection of validation errors when the error is related to model validation.
        /// </summary>
        public IEnumerable<ValidationError>? ValidationErrors { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiError"/> class.
        /// </summary>
        [JsonConstructor]
        public ApiError()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiError"/> class with the specified exception message.
        /// </summary>
        /// <param name="exceptionMessage">The exception message describing the error.</param>
        public ApiError(string exceptionMessage)
        {
            ExceptionMessage = exceptionMessage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiError"/> class with the specified exception message and validation errors.
        /// </summary>
        /// <param name="exceptionMessage">The exception message describing the error.</param>
        /// <param name="validationErrors">A collection of validation errors.</param>
        public ApiError(string exceptionMessage, IEnumerable<ValidationError> validationErrors)
        {
            ExceptionMessage = exceptionMessage;
            ValidationErrors = validationErrors;
        }
    }
}

