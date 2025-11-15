using Microsoft.EntityFrameworkCore;
using AmesaBackend.LotteryResults.Models;

namespace AmesaBackend.LotteryResults.Data
{
    public class LotteryResultsDbContext : DbContext
    {
        public LotteryResultsDbContext(DbContextOptions<LotteryResultsDbContext> options) : base(options)
        {
        }

        public DbSet<LotteryResult> LotteryResults { get; set; }
        public DbSet<LotteryResultHistory> LotteryResultHistory { get; set; }
        public DbSet<PrizeDelivery> PrizeDeliveries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LotteryResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.WinnerTicketNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PrizeType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PrizeValue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.QRCodeData).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.LotteryId);
                entity.HasIndex(e => e.DrawId);
                entity.HasIndex(e => e.WinnerUserId);
            });

            modelBuilder.Entity<LotteryResultHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Details).IsRequired().HasMaxLength(1000);
                entity.HasOne(e => e.LotteryResult).WithMany(lr => lr.History).HasForeignKey(e => e.LotteryResultId);
            });

            modelBuilder.Entity<PrizeDelivery>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RecipientName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DeliveryMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DeliveryStatus).HasMaxLength(50);
                entity.Property(e => e.ShippingCost).HasColumnType("decimal(10,2)");
                entity.HasOne(e => e.LotteryResult).WithMany(lr => lr.PrizeDeliveries).HasForeignKey(e => e.LotteryResultId);
            });
        }
    }
}

