using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Shared.Models
{
    /// <summary>
    /// Base entity class with common properties for all entities
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Timestamp when the entity was created
        /// </summary>
        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the entity was last updated
        /// </summary>
        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID who created the entity
        /// </summary>
        [Column("created_by")]
        [MaxLength(36)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User ID who last updated the entity
        /// </summary>
        [Column("updated_by")]
        [MaxLength(36)]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Timestamp when the entity was deleted (soft delete)
        /// </summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// User ID who deleted the entity
        /// </summary>
        [Column("deleted_by")]
        [MaxLength(36)]
        public string? DeletedBy { get; set; }
    }
}




