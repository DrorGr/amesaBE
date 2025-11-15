using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.LotteryResults.Models
{
    [Table("lottery_results")]
    public class LotteryResult
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid LotteryId { get; set; }

        [Required]
        public Guid DrawId { get; set; }

        [Required]
        [MaxLength(20)]
        public string WinnerTicketNumber { get; set; } = string.Empty;

        [Required]
        public Guid WinnerUserId { get; set; }

        [Required]
        public int PrizePosition { get; set; }

        [Required]
        [MaxLength(100)]
        public string PrizeType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrizeValue { get; set; }

        [MaxLength(500)]
        public string? PrizeDescription { get; set; }

        [Required]
        [MaxLength(500)]
        public string QRCodeData { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? QRCodeImageUrl { get; set; }

        public bool IsVerified { get; set; } = false;

        public bool IsClaimed { get; set; } = false;

        public DateTime? ClaimedAt { get; set; }

        [MaxLength(1000)]
        public string? ClaimNotes { get; set; }

        public DateTime ResultDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<LotteryResultHistory> History { get; set; } = new List<LotteryResultHistory>();
        public virtual ICollection<PrizeDelivery> PrizeDeliveries { get; set; } = new List<PrizeDelivery>();
    }

    [Table("lottery_result_history")]
    public class LotteryResultHistory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid LotteryResultId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PerformedBy { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public virtual LotteryResult LotteryResult { get; set; } = null!;
    }

    [Table("prize_deliveries")]
    public class PrizeDelivery
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid LotteryResultId { get; set; }

        [Required]
        public Guid WinnerUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(50)]
        public string DeliveryMethod { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        [MaxLength(50)]
        public string DeliveryStatus { get; set; } = "Pending";

        public DateTime? EstimatedDeliveryDate { get; set; }

        public DateTime? ActualDeliveryDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ShippingCost { get; set; } = 0;

        [MaxLength(1000)]
        public string? DeliveryNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual LotteryResult LotteryResult { get; set; } = null!;
    }
}

