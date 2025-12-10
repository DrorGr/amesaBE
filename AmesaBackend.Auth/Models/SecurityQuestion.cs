using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Auth.Models
{
    /// <summary>
    /// Represents a security question for account recovery.
    /// </summary>
    public class SecurityQuestion
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Question text (standardized questions)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// Hashed answer (using BCrypt)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string AnswerHash { get; set; } = string.Empty;

        /// <summary>
        /// Question order (1-3 for multiple questions)
        /// </summary>
        public int Order { get; set; } = 1;

        /// <summary>
        /// When this security question was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this security question was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User? User { get; set; }
    }
}



