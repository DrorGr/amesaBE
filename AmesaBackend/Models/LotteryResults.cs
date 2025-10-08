using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
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
        public int PrizePosition { get; set; } // 1st, 2nd, 3rd place

        [Required]
        [MaxLength(100)]
        public string PrizeType { get; set; } = string.Empty; // "House", "Cash", "Gift"

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

        // Navigation properties
        public virtual House House { get; set; } = null!;
        public virtual LotteryDraw Draw { get; set; } = null!;
        public virtual User Winner { get; set; } = null!;
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
        public string Action { get; set; } = string.Empty; // "Created", "Verified", "Claimed", "Delivered"

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

        // Navigation properties
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

        // Delivery Address
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

        // Delivery Details
        [Required]
        [MaxLength(50)]
        public string DeliveryMethod { get; set; } = string.Empty; // "Standard", "Express", "Pickup"

        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        [MaxLength(50)]
        public string DeliveryStatus { get; set; } = "Pending"; // "Pending", "Processing", "Shipped", "Delivered", "Failed"

        public DateTime? EstimatedDeliveryDate { get; set; }

        public DateTime? ActualDeliveryDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ShippingCost { get; set; } = 0;

        [MaxLength(1000)]
        public string? DeliveryNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual LotteryResult LotteryResult { get; set; } = null!;
        public virtual User Winner { get; set; } = null!;
    }

    [Table("scratch_card_results")]
    public class ScratchCardResult
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CardType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        public bool IsWinner { get; set; }

        [MaxLength(100)]
        public string? PrizeType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrizeValue { get; set; } = 0;

        [MaxLength(500)]
        public string? PrizeDescription { get; set; }

        [Required]
        [MaxLength(1000)]
        public string CardImageUrl { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ScratchedImageUrl { get; set; }

        public bool IsScratched { get; set; } = false;

        public DateTime? ScratchedAt { get; set; }

        public bool IsClaimed { get; set; } = false;

        public DateTime? ClaimedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    public enum PrizePosition
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    public enum DeliveryStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Failed,
        Cancelled
    }

    public enum ScratchCardType
    {
        Bronze,
        Silver,
        Gold,
        Platinum
    }
}


