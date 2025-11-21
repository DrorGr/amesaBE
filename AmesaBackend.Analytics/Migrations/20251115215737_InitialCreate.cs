using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmesaBackend.Analytics.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "amesa_analytics");

            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "amesa_analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Os = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_activity_logs",
                schema: "amesa_analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_activity_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_activity_logs_user_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "amesa_analytics",
                        principalTable: "user_sessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_activity_logs_CreatedAt",
                schema: "amesa_analytics",
                table: "user_activity_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_activity_logs_SessionId",
                schema: "amesa_analytics",
                table: "user_activity_logs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_activity_logs_UserId",
                schema: "amesa_analytics",
                table: "user_activity_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_SessionToken",
                schema: "amesa_analytics",
                table: "user_sessions",
                column: "SessionToken");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_UserId",
                schema: "amesa_analytics",
                table: "user_sessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_activity_logs",
                schema: "amesa_analytics");

            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "amesa_analytics");
        }
    }
}
