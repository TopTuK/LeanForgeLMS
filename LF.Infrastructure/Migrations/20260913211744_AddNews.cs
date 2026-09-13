using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LFNewsPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Html = table.Column<string>(type: "text", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFNewsPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LFNewsReadMarkers",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFNewsReadMarkers", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "LFNewsImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NewsPostId = table.Column<int>(type: "integer", nullable: false),
                    StorageObjectId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFNewsImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFNewsImages_LFNewsPosts_NewsPostId",
                        column: x => x.NewsPostId,
                        principalTable: "LFNewsPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LFNewsImages_LFStorageObjects_StorageObjectId",
                        column: x => x.StorageObjectId,
                        principalTable: "LFStorageObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFNewsImages_NewsPostId",
                table: "LFNewsImages",
                column: "NewsPostId");

            migrationBuilder.CreateIndex(
                name: "IX_LFNewsImages_StorageObjectId",
                table: "LFNewsImages",
                column: "StorageObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LFNewsPosts_IsPublished_Visibility_PublishedAt",
                table: "LFNewsPosts",
                columns: new[] { "IsPublished", "Visibility", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFNewsImages");

            migrationBuilder.DropTable(
                name: "LFNewsReadMarkers");

            migrationBuilder.DropTable(
                name: "LFNewsPosts");
        }
    }
}
