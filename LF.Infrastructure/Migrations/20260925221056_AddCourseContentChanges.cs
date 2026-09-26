using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseContentChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LFCourseContentChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    FirstChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFCourseContentChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFCourseContentChanges_LFLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "LFLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFCourseContentChanges_LessonId",
                table: "LFCourseContentChanges",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LFCourseContentChanges_NotifiedAt_CourseId",
                table: "LFCourseContentChanges",
                columns: new[] { "NotifiedAt", "CourseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFCourseContentChanges");
        }
    }
}
