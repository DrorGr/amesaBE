using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmesaBackend.Notification.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSystemEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create notification_types table
            migrationBuilder.CreateTable(
                name: "notification_types",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Feature = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultChannels = table.Column<string[]>(type: "text[]", nullable: false),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RequiresConfirmation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_types", x => x.Id);
                    table.UniqueConstraint("AK_notification_types_Code", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "idx_notification_types_code",
                schema: "amesa_notification",
                table: "notification_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notification_types_category",
                schema: "amesa_notification",
                table: "notification_types",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "idx_notification_types_feature",
                schema: "amesa_notification",
                table: "notification_types",
                column: "Feature");

            // Create notification_read_history table
            migrationBuilder.CreateTable(
                name: "notification_read_history",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    ReadMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "manual"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_read_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_read_history_user_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "amesa_notification",
                        principalTable: "user_notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_read_history_notification_id",
                schema: "amesa_notification",
                table: "notification_read_history",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "idx_read_history_user_id",
                schema: "amesa_notification",
                table: "notification_read_history",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_read_history_read_at",
                schema: "amesa_notification",
                table: "notification_read_history",
                column: "ReadAt");

            migrationBuilder.CreateIndex(
                name: "idx_read_history_channel",
                schema: "amesa_notification",
                table: "notification_read_history",
                column: "Channel");

            // Create user_feature_preferences table
            migrationBuilder.CreateTable(
                name: "user_feature_preferences",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Feature = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Channels = table.Column<string[]>(type: "text[]", nullable: false),
                    FrequencyLimit = table.Column<int>(type: "integer", nullable: true),
                    FrequencyWindow = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    QuietHoursStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    QuietHoursEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_feature_preferences", x => x.Id);
                    table.UniqueConstraint("AK_user_feature_preferences_UserId_Feature", x => new { x.UserId, x.Feature });
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_feature_preferences_user_id",
                schema: "amesa_notification",
                table: "user_feature_preferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_user_feature_preferences_feature",
                schema: "amesa_notification",
                table: "user_feature_preferences",
                column: "Feature");

            // Create user_type_preferences table
            migrationBuilder.CreateTable(
                name: "user_type_preferences",
                schema: "amesa_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationTypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Channels = table.Column<string[]>(type: "text[]", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_type_preferences", x => x.Id);
                    table.UniqueConstraint("AK_user_type_preferences_UserId_NotificationTypeCode", x => new { x.UserId, x.NotificationTypeCode });
                    table.ForeignKey(
                        name: "FK_user_type_preferences_notification_types_NotificationTypeCode",
                        column: x => x.NotificationTypeCode,
                        principalSchema: "amesa_notification",
                        principalTable: "notification_types",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_type_preferences_user_id",
                schema: "amesa_notification",
                table: "user_type_preferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_user_type_preferences_type_code",
                schema: "amesa_notification",
                table: "user_type_preferences",
                column: "NotificationTypeCode");

            // Add columns to user_notifications table
            migrationBuilder.AddColumn<string>(
                name: "NotificationTypeCode",
                schema: "amesa_notification",
                table: "user_notifications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "amesa_notification",
                table: "user_notifications",
                type: "bytea",
                nullable: true,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.CreateIndex(
                name: "idx_user_notifications_type_code",
                schema: "amesa_notification",
                table: "user_notifications",
                column: "NotificationTypeCode");

            migrationBuilder.AddForeignKey(
                name: "FK_user_notifications_notification_types_NotificationTypeCode",
                schema: "amesa_notification",
                table: "user_notifications",
                column: "NotificationTypeCode",
                principalSchema: "amesa_notification",
                principalTable: "notification_types",
                principalColumn: "Code",
                onDelete: ReferentialAction.SetNull);

            // Add columns to notification_deliveries table
            migrationBuilder.AddColumn<string>(
                name: "TrackingToken",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClickTrackingEnabled",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OpenCount",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClickCount",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOpenedAt",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastClickedAt",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BounceType",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BounceReason",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UnsubscribeRequested",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UnsubscribeReason",
                schema: "amesa_notification",
                table: "notification_deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_tracking_token",
                schema: "amesa_notification",
                table: "notification_deliveries",
                column: "TrackingToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_unsubscribe",
                schema: "amesa_notification",
                table: "notification_deliveries",
                column: "UnsubscribeRequested");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove indexes and columns from notification_deliveries
            migrationBuilder.DropIndex(
                name: "idx_notification_deliveries_unsubscribe",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropIndex(
                name: "idx_notification_deliveries_tracking_token",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "UnsubscribeReason",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "UnsubscribeRequested",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "BounceReason",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "BounceType",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "LastClickedAt",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "LastOpenedAt",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "ClickCount",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "OpenCount",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "ClickTrackingEnabled",
                schema: "amesa_notification",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "TrackingToken",
                schema: "amesa_notification",
                table: "notification_deliveries");

            // Remove foreign key and columns from user_notifications
            migrationBuilder.DropForeignKey(
                name: "FK_user_notifications_notification_types_NotificationTypeCode",
                schema: "amesa_notification",
                table: "user_notifications");

            migrationBuilder.DropIndex(
                name: "idx_user_notifications_type_code",
                schema: "amesa_notification",
                table: "user_notifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "amesa_notification",
                table: "user_notifications");

            migrationBuilder.DropColumn(
                name: "NotificationTypeCode",
                schema: "amesa_notification",
                table: "user_notifications");

            // Drop user_type_preferences table
            migrationBuilder.DropTable(
                name: "user_type_preferences",
                schema: "amesa_notification");

            // Drop user_feature_preferences table
            migrationBuilder.DropTable(
                name: "user_feature_preferences",
                schema: "amesa_notification");

            // Drop notification_read_history table
            migrationBuilder.DropTable(
                name: "notification_read_history",
                schema: "amesa_notification");

            // Drop notification_types table
            migrationBuilder.DropTable(
                name: "notification_types",
                schema: "amesa_notification");
        }
    }
}

