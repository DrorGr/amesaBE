using Microsoft.EntityFrameworkCore;
using AmesaBackend.Notification.Models;

namespace AmesaBackend.Notification.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
        public DbSet<UserChannelPreference> UserChannelPreferences { get; set; }
        public DbSet<PushSubscription> PushSubscriptions { get; set; }
        public DbSet<TelegramUserLink> TelegramUserLinks { get; set; }
        public DbSet<SocialMediaLink> SocialMediaLinks { get; set; }
        public DbSet<NotificationQueue> NotificationQueue { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NotificationTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.HasOne(e => e.Template).WithMany().HasForeignKey(e => e.TemplateId);
            });

            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(255);
            });

            modelBuilder.Entity<NotificationDelivery>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Channel).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.NotificationId).HasDatabaseName("idx_notification_deliveries_notification_id");
                entity.HasIndex(e => e.Status).HasDatabaseName("idx_notification_deliveries_status");
                entity.HasOne(e => e.Notification).WithMany().HasForeignKey(e => e.NotificationId);
            });

            modelBuilder.Entity<UserChannelPreference>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Channel).IsRequired().HasMaxLength(50);
                entity.Property(e => e.QuietHoursStart).HasColumnType("time");
                entity.Property(e => e.QuietHoursEnd).HasColumnType("time");
                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_channel_preferences_user_id");
                entity.HasIndex(e => new { e.UserId, e.Channel }).IsUnique();
            });

            modelBuilder.Entity<PushSubscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Endpoint).IsRequired();
                entity.Property(e => e.P256dhKey).IsRequired();
                entity.Property(e => e.AuthKey).IsRequired();
                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_push_subscriptions_user_id");
                entity.HasIndex(e => new { e.UserId, e.Endpoint }).IsUnique();
            });

            modelBuilder.Entity<TelegramUserLink>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.TelegramUserId).IsUnique();
            });

            modelBuilder.Entity<SocialMediaLink>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PlatformUserId).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => new { e.UserId, e.Platform }).IsUnique();
            });

            modelBuilder.Entity<NotificationQueue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Channel).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => new { e.Status, e.ScheduledFor }).HasDatabaseName("idx_notification_queue_status");
                entity.HasIndex(e => new { e.Priority, e.CreatedAt }).HasDatabaseName("idx_notification_queue_priority");
                entity.HasOne(e => e.Notification).WithMany().HasForeignKey(e => e.NotificationId);
            });
        }
    }
}

