using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmesaBackend.Lottery.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "amesa_lottery");

            migrationBuilder.CreateTable(
                name: "houses",
                schema: "amesa_lottery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    Location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Bedrooms = table.Column<int>(type: "integer", nullable: false),
                    Bathrooms = table.Column<int>(type: "integer", nullable: false),
                    SquareFeet = table.Column<int>(type: "integer", nullable: true),
                    PropertyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    YearBuilt = table.Column<int>(type: "integer", nullable: true),
                    LotSize = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Features = table.Column<string[]>(type: "text[]", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalTickets = table.Column<int>(type: "integer", nullable: false),
                    TicketPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    LotteryStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LotteryEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DrawDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MinimumParticipationPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_houses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "house_images",
                schema: "amesa_lottery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    MediaType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_house_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_house_images_houses_HouseId",
                        column: x => x.HouseId,
                        principalSchema: "amesa_lottery",
                        principalTable: "houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lottery_draws",
                schema: "amesa_lottery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrawDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalTicketsSold = table.Column<int>(type: "integer", nullable: false),
                    TotalParticipationPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    WinningTicketNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WinningTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    WinnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DrawStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DrawMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DrawSeed = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ConductedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ConductedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lottery_draws", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lottery_draws_houses_HouseId",
                        column: x => x.HouseId,
                        principalSchema: "amesa_lottery",
                        principalTable: "houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lottery_tickets",
                schema: "amesa_lottery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsWinner = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lottery_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lottery_tickets_houses_HouseId",
                        column: x => x.HouseId,
                        principalSchema: "amesa_lottery",
                        principalTable: "houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_house_images_HouseId",
                schema: "amesa_lottery",
                table: "house_images",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_houses_Status",
                schema: "amesa_lottery",
                table: "houses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_lottery_draws_DrawDate",
                schema: "amesa_lottery",
                table: "lottery_draws",
                column: "DrawDate");

            migrationBuilder.CreateIndex(
                name: "IX_lottery_draws_HouseId",
                schema: "amesa_lottery",
                table: "lottery_draws",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_lottery_tickets_HouseId",
                schema: "amesa_lottery",
                table: "lottery_tickets",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_lottery_tickets_TicketNumber",
                schema: "amesa_lottery",
                table: "lottery_tickets",
                column: "TicketNumber");

            migrationBuilder.CreateIndex(
                name: "IX_lottery_tickets_UserId",
                schema: "amesa_lottery",
                table: "lottery_tickets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "house_images",
                schema: "amesa_lottery");

            migrationBuilder.DropTable(
                name: "lottery_draws",
                schema: "amesa_lottery");

            migrationBuilder.DropTable(
                name: "lottery_tickets",
                schema: "amesa_lottery");

            migrationBuilder.DropTable(
                name: "houses",
                schema: "amesa_lottery");
        }
    }
}
