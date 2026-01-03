using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmesaBackend.Models;

namespace AmesaBackend.Lottery.Models
{
    /// <summary>
    /// Entity representing a ticket reservation in the lottery system.
    /// Reservations are temporary holds on tickets that must be confirmed through payment processing.
    /// Maps to the ticket_reservations table in the amesa_lottery schema.
    /// </summary>
    [Table("ticket_reservations", Schema = "amesa_lottery")]
    public class TicketReservation
    {
        /// <summary>
        /// Gets or sets the unique identifier of the reservation (primary key).
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the house for which tickets are reserved.
        /// </summary>
        [Required]
        public Guid HouseId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user making the reservation.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the number of tickets being reserved.
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the total price for the reserved tickets (after any discounts).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Gets or sets the optional unique identifier of the payment method to be used.
        /// </summary>
        public Guid? PaymentMethodId { get; set; }

        /// <summary>
        /// Gets or sets the status of the reservation (e.g., "pending", "confirmed", "expired", "failed").
        /// Default is "pending".
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Gets or sets the unique reservation token used to identify and secure the reservation.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ReservationToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the reservation expires.
        /// After this time, the reservation is no longer valid and tickets are released.
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the reservation was processed (confirmed or failed).
        /// Null if the reservation is still pending.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the payment transaction associated with this reservation.
        /// Set when the reservation is confirmed through payment processing.
        /// </summary>
        public Guid? PaymentTransactionId { get; set; }

        /// <summary>
        /// Gets or sets the optional promotion code applied to this reservation.
        /// </summary>
        [MaxLength(50)]
        public string? PromotionCode { get; set; }

        /// <summary>
        /// Gets or sets the discount amount applied from the promotion code.
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// Gets or sets an optional error message if the reservation processing failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the reservation was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the reservation was last updated.
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the navigation property to the house associated with this reservation.
        /// </summary>
        public virtual House House { get; set; } = null!;
    }
}
