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
        
        // User preferences tables
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<UserPreferenceHistory> UserPreferenceHistory { get; set; }
        public DbSet<UserPreferenceSyncLog> UserPreferenceSyncLog { get; set; }

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
                // Explicitly map DeletedAt to deleted_at column (nullable, may not exist in all databases)
                entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
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
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // Configure UserSession entity
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255);
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

            // Configure UserPreferences entity
            modelBuilder.Entity<UserPreferences>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.PreferencesJson).IsRequired().HasColumnType("jsonb");
                entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
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
        }
    }
}

