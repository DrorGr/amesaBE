namespace AmesaBackend.Shared.Contracts
{
    /// <summary>
    /// Represents a validation failure for a specific request field.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// The name of the field that failed validation.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable message describing the validation issue.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}

