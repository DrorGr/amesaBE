using Microsoft.EntityFrameworkCore;
using AmesaBackend.Models;

namespace AmesaBackend.Data
{
    public class AmesaDbContext : DbContext
    {
        public AmesaDbContext(DbContextOptions<AmesaDbContext> options) : base(options)
        {
        }

        // User related tables
        public DbSet<User> Users { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserPhone> UserPhones { get; set; }
        public DbSet<UserIdentityDocument> UserIdentityDocuments { get; set; }
        public DbSet<UserPaymentMethod> UserPaymentMethods { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }

        // Lottery related tables
        public DbSet<House> Houses { get; set; }
        public DbSet<HouseImage> HouseImages { get; set; }
        public DbSet<LotteryTicket> LotteryTickets { get; set; }
        public DbSet<LotteryDraw> LotteryDraws { get; set; }

        // Payment related tables
        public DbSet<Transaction> Transactions { get; set; }

        // Content related tables
        public DbSet<ContentCategory> ContentCategories { get; set; }
        public DbSet<Content> Contents { get; set; }
        public DbSet<ContentMedia> ContentMedia { get; set; }

        // Promotion related tables
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<UserPromotion> UserPromotions { get; set; }

        // Notification related tables
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }

        // System tables
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        // Translation tables
        public DbSet<Translation> Translations { get; set; }
        public DbSet<Language> Languages { get; set; }

        // Lottery Results tables
        public DbSet<LotteryResult> LotteryResults { get; set; }
        public DbSet<LotteryResultHistory> LotteryResultHistory { get; set; }
        public DbSet<PrizeDelivery> PrizeDeliveries { get; set; }
        public DbSet<ScratchCardResult> ScratchCardResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.VerificationStatus).HasConversion<string>();
                entity.Property(e => e.AuthProvider).HasConversion<string>();
                entity.Property(e => e.Gender).HasConversion<string>();
            });

            // Configure House entity
            modelBuilder.Entity<House>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TicketPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinimumParticipationPercentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.Features).HasColumnType("jsonb");
            });

            // Configure LotteryTicket entity
            modelBuilder.Entity<LotteryTicket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TicketNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.House).WithMany().HasForeignKey(e => e.HouseId);
            });

            // Configure Transaction entity
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).HasMaxLength(3);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure UserAddress entity
            modelBuilder.Entity<UserAddress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure UserPhone entity
            modelBuilder.Entity<UserPhone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure UserPaymentMethod entity
            modelBuilder.Entity<UserPaymentMethod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.CardLastFour).HasMaxLength(4);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure HouseImage entity
            modelBuilder.Entity<HouseImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImageUrl).IsRequired();
                entity.Property(e => e.MediaType).HasConversion<string>();
                entity.HasOne(e => e.House).WithMany(h => h.Images).HasForeignKey(e => e.HouseId);
            });

            // Configure LotteryDraw entity
            modelBuilder.Entity<LotteryDraw>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalParticipationPercentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.DrawStatus).HasConversion<string>();
                entity.HasOne(e => e.House).WithMany().HasForeignKey(e => e.HouseId);
                entity.HasOne(e => e.WinnerUser).WithMany().HasForeignKey(e => e.WinnerUserId);
            });

            // Configure UserSession entity
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure UserNotification entity
            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure Promotion entity
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Value).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.ApplicableHouses).HasColumnType("jsonb");
                entity.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedBy);
            });

            // Configure UserPromotion entity
            modelBuilder.Entity<UserPromotion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10,2)");
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Promotion).WithMany().HasForeignKey(e => e.PromotionId);
                entity.HasOne(e => e.Transaction).WithMany().HasForeignKey(e => e.TransactionId);
            });

            // Configure Content entity
            modelBuilder.Entity<Content>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.Language).HasMaxLength(10);
                entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId);
                entity.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId);
            });

            // Configure ContentCategory entity
            modelBuilder.Entity<ContentCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
            });

            // Configure ContentMedia entity
            modelBuilder.Entity<ContentMedia>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MediaUrl).IsRequired();
                entity.Property(e => e.MediaType).HasConversion<string>();
                entity.HasOne(e => e.Content).WithMany(c => c.Media).HasForeignKey(e => e.ContentId);
            });

            // Configure SystemSetting entity
            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Type).HasMaxLength(50);
            });

            // Configure EmailTemplate entity
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Language).HasMaxLength(10);
                entity.Property(e => e.Variables).HasColumnType("jsonb");
            });

            // Configure NotificationTemplate entity
            modelBuilder.Entity<NotificationTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Variables).HasColumnType("jsonb");
            });

            // Configure UserIdentityDocument entity
            modelBuilder.Entity<UserIdentityDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DocumentNumber).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Refund entity configuration will be added when Refund model is implemented

            // Configure Language entity
            modelBuilder.Entity<Language>(entity =>
            {
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NativeName).HasMaxLength(255);
                entity.Property(e => e.FlagUrl).HasMaxLength(10);
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure Translation entity
            modelBuilder.Entity<Translation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LanguageCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Description);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                
                // Foreign key relationship
                entity.HasOne(e => e.Language)
                      .WithMany(l => l.Translations)
                      .HasForeignKey(e => e.LanguageCode)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // Composite unique index for language code + key
                entity.HasIndex(e => new { e.LanguageCode, e.Key }).IsUnique();
            });

            // Configure LotteryResult entity
            modelBuilder.Entity<LotteryResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.WinnerTicketNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PrizeType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.QRCodeData).IsRequired().HasMaxLength(500);
                entity.Property(e => e.QRCodeImageUrl).HasMaxLength(255);
                entity.Property(e => e.PrizeDescription).HasMaxLength(500);
                entity.Property(e => e.ClaimNotes).HasMaxLength(1000);
                
                // Foreign key relationships
                entity.HasOne(e => e.House)
                      .WithMany()
                      .HasForeignKey(e => e.LotteryId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Draw)
                      .WithMany()
                      .HasForeignKey(e => e.DrawId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Winner)
                      .WithMany()
                      .HasForeignKey(e => e.WinnerUserId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                // Indexes
                entity.HasIndex(e => e.LotteryId);
                entity.HasIndex(e => e.WinnerUserId);
                entity.HasIndex(e => e.WinnerTicketNumber);
                entity.HasIndex(e => e.ResultDate);
            });

            // Configure LotteryResultHistory entity
            modelBuilder.Entity<LotteryResultHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Details).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.PerformedBy).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                
                // Foreign key relationship
                entity.HasOne(e => e.LotteryResult)
                      .WithMany(lr => lr.History)
                      .HasForeignKey(e => e.LotteryResultId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // Index
                entity.HasIndex(e => e.LotteryResultId);
                entity.HasIndex(e => e.Timestamp);
            });

            // Configure PrizeDelivery entity
            modelBuilder.Entity<PrizeDelivery>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RecipientName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AddressLine2).HasMaxLength(200);
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.State).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.DeliveryMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TrackingNumber).HasMaxLength(100);
                entity.Property(e => e.DeliveryStatus).HasMaxLength(50);
                entity.Property(e => e.DeliveryNotes).HasMaxLength(1000);
                
                // Foreign key relationships
                entity.HasOne(e => e.LotteryResult)
                      .WithMany(lr => lr.PrizeDeliveries)
                      .HasForeignKey(e => e.LotteryResultId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Winner)
                      .WithMany()
                      .HasForeignKey(e => e.WinnerUserId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                // Indexes
                entity.HasIndex(e => e.LotteryResultId);
                entity.HasIndex(e => e.WinnerUserId);
                entity.HasIndex(e => e.DeliveryStatus);
            });

            // Configure ScratchCardResult entity
            modelBuilder.Entity<ScratchCardResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CardType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PrizeType).HasMaxLength(100);
                entity.Property(e => e.PrizeDescription).HasMaxLength(500);
                entity.Property(e => e.CardImageUrl).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.ScratchedImageUrl).HasMaxLength(1000);
                
                // Foreign key relationship
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CardNumber).IsUnique();
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}
