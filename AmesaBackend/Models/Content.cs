using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
{
    [Table("content_categories")]
    public class ContentCategory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid? ParentId { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ContentCategory? Parent { get; set; }
        public virtual ICollection<ContentCategory> Children { get; set; } = new List<ContentCategory>();
        public virtual ICollection<Content> Contents { get; set; } = new List<Content>();
    }

    [Table("content")]
    public class Content
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Slug { get; set; } = string.Empty;

        public string? ContentBody { get; set; }

        public string? Excerpt { get; set; }

        public Guid? CategoryId { get; set; }

        public ContentStatus Status { get; set; } = ContentStatus.Draft;

        public Guid? AuthorId { get; set; }

        public DateTime? PublishedAt { get; set; }

        [MaxLength(255)]
        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? FeaturedImageUrl { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ContentCategory? Category { get; set; }
        public virtual User? Author { get; set; }
        public virtual ICollection<ContentMedia> Media { get; set; } = new List<ContentMedia>();
    }

    [Table("content_media")]
    public class ContentMedia
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? ContentId { get; set; }

        [Required]
        public string MediaUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AltText { get; set; }

        public string? Caption { get; set; }

        [Required]
        public MediaType MediaType { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public int? FileSize { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Content? Content { get; set; }
    }
}
