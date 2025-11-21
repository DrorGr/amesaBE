using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmesaBackend.LotteryResults.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "amesa_lottery_results");

            migrationBuilder.CreateTable(
                name: "lottery_results",
                schema: "amesa_lottery_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotteryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrawId = table.Column<Guid>(type: "uuid", nullable: false),
                    WinnerTicketNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WinnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrizePosition = table.Column<int>(type: "integer", nullable: false),
                    PrizeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrizeValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrizeDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QRCodeData = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QRCodeImageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsClaimed = table.Column<bool>(type: "boolean", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResultDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lottery_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lottery_result_history",
                schema: "amesa_lottery_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotteryResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lottery_result_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lottery_result_history_lottery_results_LotteryResultId",
                        column: x => x.LotteryResultId,
                        principalSchema: "amesa_lottery_results",
                        principalTable: "lottery_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prize_deliveries",
                schema: "amesa_lottery_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotteryResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    WinnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeliveryMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstimatedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShippingCost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DeliveryNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prize_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prize_deliveries_lottery_results_LotteryResultId",
                        column: x => x.LotteryResultId,
                        principalSchema: "amesa_lottery_results",
                        principalTable: "lottery_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lottery_result_history_LotteryResultId",
                schema: "amesa_lottery_results",
                table: "lottery_result_history",
                column: "LotteryResultId");

            migrationBuilder.CreateIndex(
                name: "IX_lottery_results_DrawId",
                schema: "amesa_lottery_results",
                table: "lottery_results",
                column: "DrawId");

            migrationBuilder.CreateIndex(
                name: "IX_lottery_results_LotteryId",
                schema: "amesa_lottery_results",
                table: "lottery_results",
                column: "LotteryId");

            migrationBuilder.CreateIndex(
                name: "IX_lottery_results_WinnerUserId",
                schema: "amesa_lottery_results",
                table: "lottery_results",
                column: "WinnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_prize_deliveries_LotteryResultId",
                schema: "amesa_lottery_results",
                table: "prize_deliveries",
                column: "LotteryResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lottery_result_history",
                schema: "amesa_lottery_results");

            migrationBuilder.DropTable(
                name: "prize_deliveries",
                schema: "amesa_lottery_results");

            migrationBuilder.DropTable(
                name: "lottery_results",
                schema: "amesa_lottery_results");
        }
    }
}
