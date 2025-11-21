using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmesaBackend.Payment.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "amesa_payment");

            migrationBuilder.CreateTable(
                name: "user_payment_methods",
                schema: "amesa_payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProviderPaymentMethodId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CardLastFour = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CardBrand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CardExpMonth = table.Column<int>(type: "integer", nullable: true),
                    CardExpYear = table.Column<int>(type: "integer", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_payment_methods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                schema: "amesa_payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderResponse = table.Column<string>(type: "jsonb", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transactions_user_payment_methods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalSchema: "amesa_payment",
                        principalTable: "user_payment_methods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_PaymentMethodId",
                schema: "amesa_payment",
                table: "transactions",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_ReferenceId",
                schema: "amesa_payment",
                table: "transactions",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_UserId",
                schema: "amesa_payment",
                table: "transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_payment_methods_UserId",
                schema: "amesa_payment",
                table: "user_payment_methods",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transactions",
                schema: "amesa_payment");

            migrationBuilder.DropTable(
                name: "user_payment_methods",
                schema: "amesa_payment");
        }
    }
}
