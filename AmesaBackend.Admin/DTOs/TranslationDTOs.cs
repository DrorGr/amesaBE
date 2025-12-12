namespace AmesaBackend.Admin.DTOs
{
    public class TranslationDto
    {
        public string Key { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTranslationRequest
    {
        public string Key { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateTranslationRequest
    {
        public string? Value { get; set; }
        public string? Category { get; set; }
    }
}

