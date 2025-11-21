using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmesaBackend.Content.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "amesa_content");

            migrationBuilder.CreateTable(
                name: "content_categories",
                schema: "amesa_content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_categories_content_categories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "amesa_content",
                        principalTable: "content_categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "amesa_content",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NativeName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FlagUrl = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_languages", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "content",
                schema: "amesa_content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentBody = table.Column<string>(type: "text", nullable: true),
                    Excerpt = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetaTitle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaDescription = table.Column<string>(type: "text", nullable: true),
                    FeaturedImageUrl = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContentCategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_content_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "amesa_content",
                        principalTable: "content_categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_content_content_categories_ContentCategoryId",
                        column: x => x.ContentCategoryId,
                        principalSchema: "amesa_content",
                        principalTable: "content_categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "translations",
                schema: "amesa_content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_translations_languages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalSchema: "amesa_content",
                        principalTable: "languages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_media",
                schema: "amesa_content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: true),
                    MediaUrl = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    MediaType = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    FileSize = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_media_content_ContentId",
                        column: x => x.ContentId,
                        principalSchema: "amesa_content",
                        principalTable: "content",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_CategoryId",
                schema: "amesa_content",
                table: "content",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_content_ContentCategoryId",
                schema: "amesa_content",
                table: "content",
                column: "ContentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_content_categories_ParentId",
                schema: "amesa_content",
                table: "content_categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_media_ContentId",
                schema: "amesa_content",
                table: "content_media",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_languages_Code",
                schema: "amesa_content",
                table: "languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_translations_LanguageCode_Key",
                schema: "amesa_content",
                table: "translations",
                columns: new[] { "LanguageCode", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_media",
                schema: "amesa_content");

            migrationBuilder.DropTable(
                name: "translations",
                schema: "amesa_content");

            migrationBuilder.DropTable(
                name: "content",
                schema: "amesa_content");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "amesa_content");

            migrationBuilder.DropTable(
                name: "content_categories",
                schema: "amesa_content");
        }
    }
}
