using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmesaBackend.Notification.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiChannelNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns to existing tables
            migrationBuilder.AddColumn<string>(
                name: "Language",
                schema: "amesa_notification",
                table: "notification_templates",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                schema: "amesa_notification",
                table: "notification_templates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "all");

            migrationBuilder.AddColumn<string>(
                name: "ChannelSpecific",
                schema: "amesa_notification",
                table: "email_templates",
                type: "jsonb",
                nullable: true);

            // Create notification_deliveries table
            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClickedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Cost = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_user_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "amesa_notification",
                        principalTable: "user_notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create user_channel_preferences table
            migrationBuilder.CreateTable(
                name: "user_channel_preferences",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotificationTypes = table.Column<string>(type: "jsonb", nullable: true),
                    QuietHoursStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    QuietHoursEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_channel_preferences", x => x.Id);
                    table.UniqueConstraint("AK_user_channel_preferences_UserId_Channel", x => new { x.UserId, x.Channel });
                });

            // Create push_subscriptions table
            migrationBuilder.CreateTable(
                name: "push_subscriptions",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    P256dhKey = table.Column<string>(type: "text", nullable: false),
                    AuthKey = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    DeviceInfo = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_subscriptions", x => x.Id);
                    table.UniqueConstraint("AK_push_subscriptions_UserId_Endpoint", x => new { x.UserId, x.Endpoint });
                });

            // Create telegram_user_links table
            migrationBuilder.CreateTable(
                name: "telegram_user_links",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramUsername = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VerificationToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_user_links", x => x.Id);
                    table.UniqueConstraint("AK_telegram_user_links_UserId", x => x.UserId);
                    table.UniqueConstraint("AK_telegram_user_links_TelegramUserId", x => x.TelegramUserId);
                });

            // Create social_media_links table
            migrationBuilder.CreateTable(
                name: "social_media_links",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlatformUserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_media_links", x => x.Id);
                    table.UniqueConstraint("AK_social_media_links_UserId_Platform", x => new { x.UserId, x.Platform });
                });

            // Create notification_queue table
            migrationBuilder.CreateTable(
                name: "notification_queue",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_queue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_queue_user_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "amesa_notification",
                        principalTable: "user_notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_notification_id",
                schema: "amesa_notification",
                table: "notification_deliveries",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_status",
                schema: "amesa_notification",
                table: "notification_deliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_user_channel_preferences_user_id",
                schema: "amesa_notification",
                table: "user_channel_preferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_push_subscriptions_user_id",
                schema: "amesa_notification",
                table: "push_subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_notification_queue_status",
                schema: "amesa_notification",
                table: "notification_queue",
                columns: new[] { "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "idx_notification_queue_priority",
                schema: "amesa_notification",
                table: "notification_queue",
                columns: new[] { "Priority", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "idx_notification_queue_priority",
                schema: "amesa_notification",
                table: "notification_queue");

            migrationBuilder.DropIndex(
                name: "idx_notification_queue_status",
                schema: "amesa_notification",
                table: "notification_queue");

            migrationBuilder.DropIndex(
                name: "idx_push_subscriptions_user_id",
                schema: "amesa_notification",
                table: "push_subscriptions");

            migrationBuilder.DropIndex(
                name: "idx_user_channel_preferences_user_id",
                schema: "amesa_notification",
                table: "user_channel_preferences");

            migrationBuilder.DropIndex(
                name: "idx_notification_deliveries_status",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropIndex(
                name: "idx_notification_deliveries_notification_id",
                schema: "amesa_notification",
                table: "notification_deliveries");

            // Drop tables
            migrationBuilder.DropTable(
                name: "notification_queue",
                schema: "amesa_notification");

            migrationBuilder.DropTable(
                name: "social_media_links",
                schema: "amesa_notification");

            migrationBuilder.DropTable(
                name: "telegram_user_links",
                schema: "amesa_notification");

            migrationBuilder.DropTable(
                name: "push_subscriptions",
                schema: "amesa_notification");

            migrationBuilder.DropTable(
                name: "user_channel_preferences",
                schema: "amesa_notification");

            migrationBuilder.DropTable(
                name: "notification_deliveries",
                schema: "amesa_notification");

            // Remove columns from existing tables
            migrationBuilder.DropColumn(
                name: "ChannelSpecific",
                schema: "amesa_notification",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "Channel",
                schema: "amesa_notification",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "Language",
                schema: "amesa_notification",
                table: "notification_templates");
        }
    }
}

