namespace AmesaBackend.Shared.Contracts
{
    public class ApiException : Exception
    {
        public int StatusCode { get; set; }
        public bool IsModelValidationError { get; set; }
        public IEnumerable<ValidationError>? Errors { get; set; }
        public string? ReferenceErrorCode { get; set; }
        public string? ReferenceDocumentLink { get; set; }
        
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

        public ApiException(IEnumerable<ValidationError> errors, int statusCode = 400)
        {
            this.IsModelValidationError = true;
            this.StatusCode = statusCode;
            this.Errors = errors;
        }

        public ApiException(System.Exception ex, int statusCode = 500) : base(ex.Message)
        {
            this.IsModelValidationError = false;
            StatusCode = statusCode;
        }
    }
}

