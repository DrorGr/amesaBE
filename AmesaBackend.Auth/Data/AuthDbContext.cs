using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Models;

namespace AmesaBackend.Auth.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        // User related tables
        public DbSet<User> Users { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserPhone> UserPhones { get; set; }
        public DbSet<UserIdentityDocument> UserIdentityDocuments { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<UserPasswordHistory> UserPasswordHistory { get; set; }
        public DbSet<UserAuditLog> UserAuditLogs { get; set; }
        public DbSet<BackupCode> BackupCodes { get; set; }
        public DbSet<SecurityQuestion> SecurityQuestions { get; set; }
        
        // User preferences tables
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<UserPreferenceHistory> UserPreferenceHistory { get; set; }
        public DbSet<UserPreferenceSyncLog> UserPreferenceSyncLog { get; set; }
        
        // System configuration tables
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

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
                // Map DeletedAt to deleted_at column (nullable, for soft deletes)
                entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
                // Add query filter for soft deletes (exclude soft-deleted users by default)
                entity.HasQueryFilter(e => e.DeletedAt == null);
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

            // Configure UserIdentityDocument entity
            modelBuilder.Entity<UserIdentityDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DocumentNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ValidationKey).IsRequired();
                entity.HasIndex(e => e.ValidationKey).IsUnique();
                entity.Property(e => e.LivenessScore).HasColumnName("liveness_score").HasColumnType("decimal(5,2)");
                entity.Property(e => e.FaceMatchScore).HasColumnName("face_match_score").HasColumnType("decimal(5,2)");
                entity.Property(e => e.VerificationProvider).HasMaxLength(50);
                entity.Property(e => e.VerificationMetadata).HasColumnType("jsonb");
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure UserSession entity
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255);
                entity.Property(e => e.RememberMe).HasDefaultValue(false);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure UserActivityLog entity
            modelBuilder.Entity<UserActivityLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Session).WithMany().HasForeignKey(e => e.SessionId);
            });

            // Configure UserPasswordHistory entity
            modelBuilder.Entity<UserPasswordHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            });

            // Configure UserAuditLog entity
            modelBuilder.Entity<UserAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => new { e.EventType, e.CreatedAt });
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure BackupCode entity
            modelBuilder.Entity<BackupCode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CodeHash).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => new { e.UserId, e.IsUsed });
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SecurityQuestion entity
            modelBuilder.Entity<SecurityQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Question).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AnswerHash).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => new { e.UserId, e.Order });
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UserPreferences entity
            modelBuilder.Entity<UserPreferences>(entity =>
            {
                entity.ToTable("user_preferences", "amesa_auth");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.PreferencesJson).IsRequired().HasColumnType("jsonb");
                entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
                // Configure UpdatedAt as concurrency token to prevent race conditions
                entity.Property(e => e.UpdatedAt).IsConcurrencyToken();
                // Ignore BaseEntity properties that don't exist in user_preferences table
                entity.Ignore(e => e.DeletedAt);
                entity.Ignore(e => e.DeletedBy);
                entity.Ignore(e => e.IsDeleted);
                // Configure foreign key without requiring User entity to be loaded
                // This prevents EF Core from querying User table during SaveChangesAsync validation
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                // Explicitly tell EF Core not to include User in queries unless explicitly requested
                entity.Navigation(e => e.User).AutoInclude(false);
            });

            // Configure UserPreferenceHistory entity
            modelBuilder.Entity<UserPreferenceHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PropertyName).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.UserPreferences).WithMany(p => p.History).HasForeignKey(e => e.UserPreferencesId);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure UserPreferenceSyncLog entity
            modelBuilder.Entity<UserPreferenceSyncLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SyncType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SyncStatus).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.UserPreferences).WithMany().HasForeignKey(e => e.UserPreferencesId);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure SystemConfiguration entity
            modelBuilder.Entity<SystemConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired().HasColumnType("jsonb");
                entity.ToTable("system_configurations", "amesa_auth");
            });
        }
    }
}

