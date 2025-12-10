namespace AmesaBackend.Auth.DTOs
{
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public PasswordStrength Strength { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string>? Warnings { get; set; } // Warnings that don't block password (e.g., breach warnings when not blocking)
    }

    public enum PasswordStrength
    {
        Weak,
        Medium,
        Strong,
        VeryStrong
    }
}

