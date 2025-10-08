using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
{
    [Table("translations")]
    public class Translation
    {
        [Key]
        public Guid Id { get; set; }

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

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public virtual Language Language { get; set; } = null!;
    }

    [Table("languages")]
    public class Language
    {
        [Key]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? NativeName { get; set; }

        [MaxLength(10)]
        public string? FlagUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Translation> Translations { get; set; } = new List<Translation>();
    }
}
