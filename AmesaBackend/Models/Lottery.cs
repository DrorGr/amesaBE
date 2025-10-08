using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
{
    [Table("houses")]
    public class House
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Required]
        public int Bedrooms { get; set; }

        [Required]
        public int Bathrooms { get; set; }

        public int? SquareFeet { get; set; }

        [MaxLength(50)]
        public string? PropertyType { get; set; }

        public int? YearBuilt { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? LotSize { get; set; }

        public string[]? Features { get; set; }

        public LotteryStatus Status { get; set; } = LotteryStatus.Upcoming;

        [Required]
        public int TotalTickets { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TicketPrice { get; set; }

        public DateTime? LotteryStartDate { get; set; }

        [Required]
        public DateTime LotteryEndDate { get; set; }

        public DateTime? DrawDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal MinimumParticipationPercentage { get; set; } = 75.00m;

        public Guid? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ICollection<HouseImage> Images { get; set; } = new List<HouseImage>();
        public virtual ICollection<LotteryTicket> Tickets { get; set; } = new List<LotteryTicket>();
        public virtual ICollection<LotteryDraw> Draws { get; set; } = new List<LotteryDraw>();
        public virtual User? CreatedByUser { get; set; }
    }

    [Table("house_images")]
    public class HouseImage
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid HouseId { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AltText { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;

        public MediaType MediaType { get; set; } = MediaType.Image;

        public int? FileSize { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual House House { get; set; } = null!;
    }

    [Table("lottery_tickets")]
    public class LotteryTicket
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        [Required]
        public Guid HouseId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PurchasePrice { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Active;

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public Guid? PaymentId { get; set; }

        public bool IsWinner { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual House House { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual Transaction? Payment { get; set; }
    }

    [Table("lottery_draws")]
    public class LotteryDraw
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid HouseId { get; set; }

        [Required]
        public DateTime DrawDate { get; set; }

        [Required]
        public int TotalTicketsSold { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalParticipationPercentage { get; set; }

        [MaxLength(20)]
        public string? WinningTicketNumber { get; set; }

        public Guid? WinningTicketId { get; set; }

        public Guid? WinnerUserId { get; set; }

        public DrawStatus DrawStatus { get; set; } = DrawStatus.Pending;

        [MaxLength(50)]
        public string DrawMethod { get; set; } = "random";

        [MaxLength(255)]
        public string? DrawSeed { get; set; }

        public Guid? ConductedBy { get; set; }

        public DateTime? ConductedAt { get; set; }

        [MaxLength(255)]
        public string? VerificationHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual House House { get; set; } = null!;
        public virtual LotteryTicket? WinningTicket { get; set; }
        public virtual User? WinnerUser { get; set; }
        public virtual User? ConductedByUser { get; set; }
    }
}
