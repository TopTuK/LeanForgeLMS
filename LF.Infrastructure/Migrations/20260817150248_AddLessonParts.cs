using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LFLessonParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Html = table.Column<string>(type: "text", nullable: true),
                    StorageObjectId = table.Column<int>(type: "integer", nullable: true),
                    LessonId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFLessonParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFLessonParts_LFLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "LFLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LFLessonParts_LFStorageObjects_StorageObjectId",
                        column: x => x.StorageObjectId,
                        principalTable: "LFStorageObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonParts_LessonId",
                table: "LFLessonParts",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonParts_StorageObjectId",
                table: "LFLessonParts",
                column: "StorageObjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFLessonParts");
        }
    }
}
