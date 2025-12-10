using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Lottery.Models
{
    [Table("ticket_reservations", Schema = "amesa_lottery")]
    public class TicketReservation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid HouseId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        public Guid? PaymentMethodId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "pending";

        [Required]
        [MaxLength(255)]
        public string ReservationToken { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public Guid? PaymentTransactionId { get; set; }

        public string? ErrorMessage { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual House House { get; set; } = null!;
    }
}












