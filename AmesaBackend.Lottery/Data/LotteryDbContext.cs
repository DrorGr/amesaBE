using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Models;

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
        }
    }
}
