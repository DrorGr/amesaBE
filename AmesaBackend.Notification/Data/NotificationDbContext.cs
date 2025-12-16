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
        public DbSet<DeviceRegistration> DeviceRegistrations { get; set; }
        public DbSet<TelegramUserLink> TelegramUserLinks { get; set; }
        public DbSet<SocialMediaLink> SocialMediaLinks { get; set; }
        public DbSet<NotificationQueue> NotificationQueue { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<NotificationReadHistory> NotificationReadHistories { get; set; }
        public DbSet<UserFeaturePreference> UserFeaturePreferences { get; set; }
        public DbSet<UserTypePreference> UserTypePreferences { get; set; }
        public DbSet<NotificationDeliveryStatusHistory> NotificationDeliveryStatusHistories { get; set; }

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
                entity.Property(e => e.NotificationTypeCode).HasMaxLength(100);
                entity.Property(e => e.RowVersion).IsRowVersion().HasDefaultValueSql("gen_random_bytes(8)");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.DeletedBy).HasMaxLength(255);
                entity.HasOne(e => e.Template).WithMany().HasForeignKey(e => e.TemplateId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.NotificationType).WithMany(n => n.UserNotifications).HasForeignKey(e => e.NotificationTypeCode).HasPrincipalKey(n => n.Code).OnDelete(DeleteBehavior.SetNull);
                
                // Performance indexes for common queries
                entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                    .HasDatabaseName("idx_user_notifications_user_created");
                
                entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt })
                    .HasDatabaseName("idx_user_notifications_user_read_created");
                
                entity.HasIndex(e => new { e.UserId, e.Type, e.CreatedAt })
                    .HasDatabaseName("idx_user_notifications_user_type_created");
                
                entity.HasIndex(e => new { e.NotificationTypeCode, e.CreatedAt })
                    .HasDatabaseName("idx_user_notifications_type_created")
                    .HasFilter("\"NotificationTypeCode\" IS NOT NULL");
                
                entity.HasIndex(e => new { e.UserId, e.IsRead })
                    .HasDatabaseName("idx_user_notifications_user_read");
                
                entity.HasIndex(e => new { e.NotificationTypeCode, e.IsRead, e.CreatedAt })
                    .HasDatabaseName("idx_user_notifications_type_read")
                    .HasFilter("\"NotificationTypeCode\" IS NOT NULL");
                
                // Index for soft delete queries
                entity.HasIndex(e => new { e.IsDeleted, e.CreatedAt })
                    .HasDatabaseName("idx_user_notifications_deleted")
                    .HasFilter("\"IsDeleted\" = FALSE");
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
                entity.Property(e => e.TrackingToken).HasMaxLength(255);
                entity.Property(e => e.BounceType).HasMaxLength(50);
                entity.HasIndex(e => e.NotificationId).HasDatabaseName("idx_notification_deliveries_notification_id");
                entity.HasIndex(e => e.Status).HasDatabaseName("idx_notification_deliveries_status");
                entity.HasIndex(e => e.TrackingToken).HasDatabaseName("idx_notification_deliveries_tracking_token").IsUnique();
                entity.HasIndex(e => e.UnsubscribeRequested).HasDatabaseName("idx_notification_deliveries_unsubscribe");
                entity.HasOne(e => e.Notification).WithMany().HasForeignKey(e => e.NotificationId).OnDelete(DeleteBehavior.Cascade);
                
                // Additional performance indexes
                entity.HasIndex(e => new { e.NotificationId, e.Status })
                    .HasDatabaseName("idx_notification_deliveries_notification_status");
                
                entity.HasIndex(e => new { e.Channel, e.Status, e.CreatedAt })
                    .HasDatabaseName("idx_notification_deliveries_channel_status");
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

            modelBuilder.Entity<DeviceRegistration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.DeviceToken).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DeviceId).HasMaxLength(255);
                entity.Property(e => e.DeviceName).HasMaxLength(255);
                entity.Property(e => e.AppVersion).HasMaxLength(50);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.DeviceToken);
                entity.HasIndex(e => new { e.UserId, e.DeviceToken }).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.Platform });
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
                entity.HasOne(e => e.Notification).WithMany().HasForeignKey(e => e.NotificationId).OnDelete(DeleteBehavior.Cascade);
                
                // Additional index for scheduled notifications
                entity.HasIndex(e => new { e.Status, e.ScheduledFor })
                    .HasDatabaseName("idx_notification_queue_status_scheduled")
                    .HasFilter("\"ScheduledFor\" IS NOT NULL");
            });

            modelBuilder.Entity<NotificationType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Feature).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("idx_notification_types_code");
                entity.HasIndex(e => e.Category).HasDatabaseName("idx_notification_types_category");
                entity.HasIndex(e => e.Feature).HasDatabaseName("idx_notification_types_feature");
            });

            modelBuilder.Entity<NotificationReadHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DeviceId).HasMaxLength(255);
                entity.Property(e => e.DeviceName).HasMaxLength(255);
                entity.Property(e => e.Channel).HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.ReadMethod).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.NotificationId).HasDatabaseName("idx_read_history_notification_id");
                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_read_history_user_id");
                entity.HasIndex(e => e.ReadAt).HasDatabaseName("idx_read_history_read_at");
                entity.HasIndex(e => e.Channel).HasDatabaseName("idx_read_history_channel");
                entity.HasOne(e => e.Notification).WithMany(n => n.ReadHistory).HasForeignKey(e => e.NotificationId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserFeaturePreference>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Feature).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FrequencyWindow).HasMaxLength(20);
                entity.Property(e => e.QuietHoursStart).HasColumnType("time");
                entity.Property(e => e.QuietHoursEnd).HasColumnType("time");
                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_feature_preferences_user_id");
                entity.HasIndex(e => e.Feature).HasDatabaseName("idx_user_feature_preferences_feature");
                entity.HasIndex(e => new { e.UserId, e.Feature }).IsUnique().HasDatabaseName("idx_user_feature_preferences_user_feature");
            });

            modelBuilder.Entity<UserTypePreference>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NotificationTypeCode).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_type_preferences_user_id");
                entity.HasIndex(e => e.NotificationTypeCode).HasDatabaseName("idx_user_type_preferences_type_code");
                entity.HasIndex(e => new { e.UserId, e.NotificationTypeCode }).IsUnique().HasDatabaseName("idx_user_type_preferences_user_type");
                entity.HasOne(e => e.NotificationType).WithMany(t => t.UserTypePreferences).HasForeignKey(e => e.NotificationTypeCode).HasPrincipalKey(t => t.Code).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NotificationDeliveryStatusHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ChangedAt).IsRequired().HasDefaultValueSql("NOW()");
                entity.Property(e => e.ChangedBy).HasMaxLength(255);
                entity.HasOne(e => e.Delivery).WithMany().HasForeignKey(e => e.DeliveryId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.DeliveryId, e.ChangedAt }).HasDatabaseName("idx_delivery_status_history_delivery_id");
                entity.HasIndex(e => new { e.Status, e.ChangedAt }).HasDatabaseName("idx_delivery_status_history_status");
            });
        }
    }
}

