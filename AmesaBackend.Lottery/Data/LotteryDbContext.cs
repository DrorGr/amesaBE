using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Models;
using PromotionModel = AmesaBackend.Models.Promotion;
using UserPromotionModel = AmesaBackend.Models.UserPromotion;

namespace AmesaBackend.Lottery.Data
{
    public class LotteryDbContext : DbContext
    {
        public LotteryDbContext(DbContextOptions<LotteryDbContext> options) : base(options)
        {
        }

        public DbSet<House> Houses { get; set; }
        public DbSet<HouseImage> HouseImages { get; set; }
        public DbSet<LotteryTicket> LotteryTickets { get; set; }
        public DbSet<LotteryDraw> LotteryDraws { get; set; }
        public DbSet<UserWatchlist> UserWatchlist { get; set; }
        public DbSet<LotteryParticipants> LotteryParticipants { get; set; }
        public DbSet<TicketReservation> TicketReservations { get; set; }
        public DbSet<PromotionUsageAudit> PromotionUsageAudits { get; set; }
        
        // Promotion entities from amesa_admin schema
        public DbSet<PromotionModel> Promotions { get; set; }
        public DbSet<UserPromotionModel> UserPromotions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<House>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Price).HasColumnType("decimal(15,2)");
                entity.Property(e => e.TicketPrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.MinimumParticipationPercentage).HasColumnType("decimal(5,2)");
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<HouseImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImageUrl).IsRequired();
                entity.Property(e => e.MediaType).HasMaxLength(50);
                entity.HasOne(e => e.House).WithMany(h => h.Images).HasForeignKey(e => e.HouseId);
            });

            modelBuilder.Entity<LotteryTicket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TicketNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(10,2)");
                entity.HasIndex(e => e.HouseId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TicketNumber);
                // Add unique constraint on TicketNumber per house to prevent duplicates
                entity.HasIndex(e => new { e.HouseId, e.TicketNumber }).IsUnique();
                entity.HasOne(e => e.House).WithMany(h => h.Tickets).HasForeignKey(e => e.HouseId);
            });

            modelBuilder.Entity<LotteryDraw>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DrawStatus).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DrawMethod).HasMaxLength(50);
                entity.Property(e => e.TotalParticipationPercentage).HasColumnType("decimal(5,2)");
                entity.HasIndex(e => e.HouseId);
                entity.HasIndex(e => e.DrawDate);
                entity.HasOne(e => e.House).WithMany(h => h.Draws).HasForeignKey(e => e.HouseId);
            });

            modelBuilder.Entity<UserWatchlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.HouseId);
                entity.HasIndex(e => new { e.UserId, e.HouseId }).IsUnique();
                entity.HasOne(e => e.House).WithMany().HasForeignKey(e => e.HouseId);
            });

            modelBuilder.Entity<LotteryParticipants>(entity =>
            {
                entity.ToView("lottery_participants", "amesa_lottery");
                entity.HasNoKey(); // View has no primary key
            });

            modelBuilder.Entity<TicketReservation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReservationToken).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");
                entity.HasIndex(e => new { e.HouseId, e.Status });
                entity.HasIndex(e => new { e.UserId, e.Status });
                entity.HasIndex(e => e.ExpiresAt).HasFilter($"[Status] = 'pending'");
                entity.HasIndex(e => e.ReservationToken).IsUnique();
                entity.HasOne(e => e.House).WithMany().HasForeignKey(e => e.HouseId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PromotionUsageAudit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionId).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.PromotionCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DiscountAmount).IsRequired().HasColumnType("decimal(10,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ResolutionNotes).HasMaxLength(1000);
                entity.HasIndex(e => e.TransactionId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.Status, e.CreatedAt });
            });

            // Configure Promotion entity (from amesa_admin schema)
            modelBuilder.Entity<PromotionModel>(entity =>
            {
                entity.ToTable("promotions", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code);
                entity.HasIndex(e => e.IsActive);
                // Ignore legacy navigation properties to keep this context focused
                entity.Ignore(e => e.CreatedByUser);
                entity.Ignore(e => e.UserPromotions);
            });

            // Configure UserPromotion entity (from amesa_admin schema)
            modelBuilder.Entity<UserPromotionModel>(entity =>
            {
                entity.ToTable("user_promotions", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.PromotionId);
                entity.HasIndex(e => new { e.UserId, e.PromotionId });
                // Ignore navigation properties not mapped in this context
                entity.Ignore(e => e.User);
                entity.Ignore(e => e.Promotion);
                entity.Ignore(e => e.Transaction);
            });
        }
    }
}
