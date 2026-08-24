using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonPartFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LFLessonPartFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    StorageObjectId = table.Column<int>(type: "integer", nullable: false),
                    LessonPartId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFLessonPartFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFLessonPartFiles_LFLessonParts_LessonPartId",
                        column: x => x.LessonPartId,
                        principalTable: "LFLessonParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LFLessonPartFiles_LFStorageObjects_StorageObjectId",
                        column: x => x.StorageObjectId,
                        principalTable: "LFStorageObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonPartFiles_LessonPartId",
                table: "LFLessonPartFiles",
                column: "LessonPartId");

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonPartFiles_StorageObjectId",
                table: "LFLessonPartFiles",
                column: "StorageObjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFLessonPartFiles");
        }
    }
}
