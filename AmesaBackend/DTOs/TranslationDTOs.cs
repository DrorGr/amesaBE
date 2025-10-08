using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.DTOs
{
    public class TranslationDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
    }

    public class LanguageDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? NativeName { get; set; }
        public string? FlagUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class TranslationsResponseDto
    {
        public string LanguageCode { get; set; } = string.Empty;
        public Dictionary<string, string> Translations { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class CreateTranslationRequest
    {
        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Category { get; set; }
    }

    public class UpdateTranslationRequest
    {
        [Required]
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateLanguageRequest
    {
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? NativeName { get; set; }
        public string? FlagUrl { get; set; }
        public bool IsDefault { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateLanguageRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? NativeName { get; set; }
        public string? FlagUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }
}
