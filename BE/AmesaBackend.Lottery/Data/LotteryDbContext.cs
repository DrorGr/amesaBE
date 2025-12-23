using Microsoft.EntityFrameworkCore;
using AmesaBackend.Models;

namespace AmesaBackend.Lottery.Data;

public class LotteryDbContext : DbContext
{
    public LotteryDbContext(DbContextOptions<LotteryDbContext> options)
        : base(options)
    {
    }

    public DbSet<House> Houses { get; set; }
    public DbSet<LotteryTicket> LotteryTickets { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<UserPromotion> UserPromotions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema
        modelBuilder.HasDefaultSchema("amesa_lottery");

        // Configure House entity
        modelBuilder.Entity<House>(entity =>
        {
            entity.ToTable("houses", "amesa_lottery");
            entity.HasKey(e => e.Id);
        });

        // Configure LotteryTicket entity
        modelBuilder.Entity<LotteryTicket>(entity =>
        {
            entity.ToTable("lottery_tickets", "amesa_lottery");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.House)
                .WithMany()
                .HasForeignKey(e => e.HouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Promotion entity (if it exists in amesa_admin schema)
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("promotions", "amesa_admin");
            entity.HasKey(e => e.Id);
        });

        // Configure UserPromotion entity
        modelBuilder.Entity<UserPromotion>(entity =>
        {
            entity.ToTable("user_promotions", "amesa_admin");
            entity.HasKey(e => e.Id);
        });
    }
}

