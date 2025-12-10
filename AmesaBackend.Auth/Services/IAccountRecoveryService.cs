namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for account recovery operations.
    /// </summary>
    public interface IAccountRecoveryService
    {
        /// <summary>
        /// Initiates account recovery via email.
        /// </summary>
        Task<bool> InitiateEmailRecoveryAsync(string email);

        /// <summary>
        /// Initiates account recovery via phone (SMS).
        /// </summary>
        Task<bool> InitiatePhoneRecoveryAsync(string phone);

        /// <summary>
        /// Verifies security question answer.
        /// </summary>
        Task<bool> VerifySecurityQuestionAsync(Guid userId, string question, string answer);

        /// <summary>
        /// Gets available recovery methods for a user.
        /// </summary>
        Task<RecoveryMethodsResponse> GetRecoveryMethodsAsync(string identifier);

        /// <summary>
        /// Sets up security questions for a user.
        /// </summary>
        Task SetupSecurityQuestionsAsync(Guid userId, List<SecurityQuestionRequest> questions);

        /// <summary>
        /// Verifies recovery code (from email or SMS).
        /// </summary>
        Task<bool> VerifyRecoveryCodeAsync(string identifier, string code, RecoveryMethod method);
    }

    /// <summary>
    /// Available recovery methods.
    /// </summary>
    public enum RecoveryMethod
    {
        Email,
        Phone,
        SecurityQuestion
    }

    /// <summary>
    /// Response containing available recovery methods.
    /// </summary>
    public class RecoveryMethodsResponse
    {
        public bool HasEmail { get; set; }
        public bool HasPhone { get; set; }
        public bool HasSecurityQuestions { get; set; }
        public string? MaskedEmail { get; set; }
        public string? MaskedPhone { get; set; }
    }

    /// <summary>
    /// Request to set up security questions.
    /// </summary>
    public class SecurityQuestionRequest
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int Order { get; set; } = 1;
    }
}



