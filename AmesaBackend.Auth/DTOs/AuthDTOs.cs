using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Auth.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone number must be in E.164 format (e.g., +1234567890)")]
        public string? Phone { get; set; }

        public string AuthProvider { get; set; } = "email";

        public string? CaptchaToken { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// If true, creates a persistent session with longer expiration (30 days).
        /// If false, creates a session-based session with shorter expiration (7 days).
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public bool RequiresEmailVerification { get; set; } = false;
        public bool RequiresTwoFactor { get; set; } = false;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        /// <summary>
        /// Password reset token (for traditional password reset flow).
        /// Either Token OR (RecoveryCode + Identifier + RecoveryMethod) must be provided.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Recovery code (for account recovery flow).
        /// Required if Token is not provided.
        /// </summary>
        public string? RecoveryCode { get; set; }

        /// <summary>
        /// Recovery method: "email" or "phone".
        /// Required if RecoveryCode is provided.
        /// </summary>
        public string? RecoveryMethod { get; set; }

        /// <summary>
        /// User identifier (email or phone) for recovery flow.
        /// Required if RecoveryCode is provided.
        /// </summary>
        public string? Identifier { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class ResendVerificationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyPhoneRequest
    {
        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;
    }

    public class ExchangeTokenRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class UsernameAvailabilityRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;
    }

    public class UsernameAvailabilityResponse
    {
        public bool Available { get; set; }
        public List<string>? Suggestions { get; set; }
    }

    public class VerifyTwoFactorRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyTwoFactorLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class TwoFactorStatusResponse
    {
        public bool IsEnabled { get; set; }
    }

    public class TwoFactorSetupResponse
    {
        public string Secret { get; set; } = string.Empty;
        public string QrCodeImageUrl { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
    }

    public class InitiateRecoveryRequest
    {
        [Required]
        public string Identifier { get; set; } = string.Empty; // Email or phone

        [Required]
        public string Method { get; set; } = string.Empty; // "email", "phone", "securityQuestion"
    }

    public class VerifyRecoveryRequest
    {
        [Required]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Method { get; set; } = string.Empty;
    }

    public class VerifySecurityQuestionRequest
    {
        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;
    }

    public class SetupSecurityQuestionsRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(3)]
        public List<SecurityQuestionDto> Questions { get; set; } = new List<SecurityQuestionDto>();
    }

    public class SecurityQuestionDto
    {
        [Required]
        [MaxLength(255)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Answer { get; set; } = string.Empty;

        public int Order { get; set; } = 1;
    }

    public class RecoveryMethodsResponse
    {
        public bool HasEmail { get; set; }
        public bool HasPhone { get; set; }
        public bool HasSecurityQuestions { get; set; }
        public string? MaskedEmail { get; set; }
        public string? MaskedPhone { get; set; }
    }

    public class DeleteAccountRequest
    {
        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public bool ConfirmDeletion { get; set; } = false;
    }

    public class AccountDeletionStatusResponse
    {
        public bool IsPending { get; set; }
        public DateTime? DeletionDate { get; set; }
        public int DaysRemaining { get; set; }
    }
}

