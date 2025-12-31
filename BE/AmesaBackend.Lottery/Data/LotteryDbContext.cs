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
    public DbSet<UserGamification> UserGamification { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<PointsHistory> PointsHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema
        modelBuilder.HasDefaultSchema("amesa_lottery");

        // Configure House entity
        // Note: houses table uses PascalCase column names (Id, Name, Description, etc.)
        // EF Core will use property names as column names by default, which should match
        modelBuilder.Entity<House>(entity =>
        {
            entity.ToTable("houses", "amesa_lottery");
            entity.HasKey(e => e.Id);
            // Explicitly map common properties to ensure correct column names
            // If queries fail, add more explicit mappings here based on actual database schema
        });

        // Configure LotteryTicket entity
        // Note: lottery_tickets table uses PascalCase column names (Id, TicketNumber, HouseId, UserId, etc.)
        // BUT also has snake_case columns: promotion_code, discount_amount
        // Explicitly map to correct column names to ensure EF Core uses correct column names
        modelBuilder.Entity<LotteryTicket>(entity =>
        {
            entity.ToTable("lottery_tickets", "amesa_lottery");
            entity.HasKey(e => e.Id);
            // Explicitly map to PascalCase column names (matching database schema)
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.HouseId).HasColumnName("HouseId");
            entity.Property(e => e.PurchasePrice).HasColumnName("PurchasePrice");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
            entity.Property(e => e.TicketNumber).HasColumnName("TicketNumber");
            entity.Property(e => e.Status).HasColumnName("Status");
            // Note: Status is an enum, EF Core will automatically convert to/from string if database column is text type
            // Map properties that exist in the model (based on database schema: PurchaseDate, PaymentId, IsWinner are PascalCase)
            entity.Property(e => e.PurchaseDate).HasColumnName("PurchaseDate");
            entity.Property(e => e.PaymentId).HasColumnName("PaymentId");
            entity.Property(e => e.IsWinner).HasColumnName("IsWinner");
            // CRITICAL FIX: promotion_code and discount_amount exist in database but NOT in LotteryTicket model
            // EF Core tries to SELECT them when doing .ToListAsync(), causing "column does not exist" errors
            // Solution: Add shadow properties to map these columns without adding them to the model
            entity.Property<string>("PromotionCode").HasColumnName("promotion_code");
            entity.Property<decimal?>("DiscountAmount").HasColumnName("discount_amount");
            entity.HasOne(e => e.House)
                .WithMany()
                .HasForeignKey(e => e.HouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Promotion entity (if it exists in amesa_admin schema)
        // Note: promotions table uses snake_case column names (like user_promotions in same schema)
        // Based on SQL validation: table has id, title, description, type, value, value_type, code, is_active,
        // start_date, end_date, usage_limit, usage_count, min_purchase_amount, max_discount_amount,
        // applicable_houses, created_by, created_at, updated_at
        // Only mapping properties that are actually used in the codebase
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("promotions", "amesa_admin");
            entity.HasKey(e => e.Id);
            // Map properties to snake_case column names (matching amesa_admin schema convention)
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.UsageCount).HasColumnName("usage_count");
            entity.Property(e => e.ApplicableHouses).HasColumnName("applicable_houses");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
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

        // Configure UserGamification entity
        modelBuilder.Entity<UserGamification>(entity =>
        {
            entity.ToTable("user_gamification", "amesa_lottery");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TotalPoints).HasColumnName("total_points");
            entity.Property(e => e.CurrentLevel).HasColumnName("current_level");
            entity.Property(e => e.CurrentTier).HasColumnName("current_tier");
            entity.Property(e => e.CurrentStreak).HasColumnName("current_streak");
            entity.Property(e => e.LongestStreak).HasColumnName("longest_streak");
            entity.Property(e => e.LastEntryDate).HasColumnName("last_entry_date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Configure UserAchievement entity
        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.ToTable("user_achievements", "amesa_lottery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AchievementType).HasColumnName("achievement_type");
            entity.Property(e => e.AchievementName).HasColumnName("achievement_name");
            entity.Property(e => e.AchievementIcon).HasColumnName("achievement_icon");
            entity.Property(e => e.UnlockedAt).HasColumnName("unlocked_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            // Add index for user lookups
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_user_achievements_user_id");
        });

        // Configure PointsHistory entity
        modelBuilder.Entity<PointsHistory>(entity =>
        {
            entity.ToTable("points_history", "amesa_lottery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PointsChange).HasColumnName("points_change");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            // Add index for user lookups
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_points_history_user_id");
        });
    }
}

