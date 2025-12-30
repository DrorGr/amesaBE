using Microsoft.EntityFrameworkCore;
using AmesaBackend.Models;
using AmesaBackend.Lottery.Models;

namespace AmesaBackend.Lottery.Data;

public class LotteryDbContext : DbContext
{
    public LotteryDbContext(DbContextOptions<LotteryDbContext> options)
        : base(options)
    {
    }

    public DbSet<House> Houses { get; set; }
    public DbSet<LotteryTicket> LotteryTickets { get; set; }
    public DbSet<LotteryDraw> LotteryDraws { get; set; }
    public DbSet<TicketReservation> TicketReservations { get; set; }
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
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.HouseId).HasColumnName("house_id");
            entity.Property(e => e.PurchasePrice).HasColumnName("purchase_price");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.TicketNumber).HasColumnName("ticket_number");
            entity.Property(e => e.Status).HasColumnName("status");
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
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
        });

        // Configure TicketReservation entity
        modelBuilder.Entity<TicketReservation>(entity =>
        {
            entity.ToTable("ticket_reservations", "amesa_lottery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HouseId).HasColumnName("house_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");
            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.ReservationToken).HasColumnName("reservation_token");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.PaymentTransactionId).HasColumnName("payment_transaction_id");
            entity.Property(e => e.PromotionCode).HasColumnName("promotion_code");
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.House)
                .WithMany()
                .HasForeignKey(e => e.HouseId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Add indexes for performance
            entity.HasIndex(e => new { e.ExpiresAt, e.Status })
                .HasDatabaseName("IX_ticket_reservations_ExpiresAt_Status");
        });

        // Configure LotteryDraw entity
        modelBuilder.Entity<LotteryDraw>(entity =>
        {
            entity.ToTable("lottery_draws", "amesa_lottery");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.House)
                .WithMany(h => h.Draws)
                .HasForeignKey(e => e.HouseId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Add indexes for performance
            entity.HasIndex(e => new { e.DrawDate, e.DrawStatus })
                .HasDatabaseName("IX_lottery_draws_DrawDate_Status");
        });
    }
}

