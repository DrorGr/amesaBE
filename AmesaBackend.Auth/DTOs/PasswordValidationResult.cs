namespace AmesaBackend.Auth.DTOs
{
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public PasswordStrength Strength { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public enum PasswordStrength
    {
        Weak,
        Medium,
        Strong,
        VeryStrong
    }
}

