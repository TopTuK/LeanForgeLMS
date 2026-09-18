using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonQna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LFCourseInstructors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFCourseInstructors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFCourseInstructors_LFCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "LFCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFLessonQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    StudentUserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageAuthorUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFLessonQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFLessonQuestions_LFCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "LFCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LFLessonQuestions_LFLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "LFLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFLessonQuestionMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LessonQuestionId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<int>(type: "integer", nullable: false),
                    AuthorRole = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFLessonQuestionMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFLessonQuestionMessages_LFLessonQuestions_LessonQuestionId",
                        column: x => x.LessonQuestionId,
                        principalTable: "LFLessonQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFLessonQuestionReadMarkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LessonQuestionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFLessonQuestionReadMarkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFLessonQuestionReadMarkers_LFLessonQuestions_LessonQuestio~",
                        column: x => x.LessonQuestionId,
                        principalTable: "LFLessonQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFCourseInstructors_CourseId_UserId",
                table: "LFCourseInstructors",
                columns: new[] { "CourseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LFCourseInstructors_UserId",
                table: "LFCourseInstructors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonQuestionMessages_LessonQuestionId_CreatedAt",
                table: "LFLessonQuestionMessages",
                columns: new[] { "LessonQuestionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonQuestionReadMarkers_LessonQuestionId_UserId",
                table: "LFLessonQuestionReadMarkers",
                columns: new[] { "LessonQuestionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonQuestionReadMarkers_UserId",
                table: "LFLessonQuestionReadMarkers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonQuestions_CourseId_LastMessageAt",
                table: "LFLessonQuestions",
                columns: new[] { "CourseId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonQuestions_LessonId",
                table: "LFLessonQuestions",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LFLessonQuestions_StudentUserId_LastMessageAt",
                table: "LFLessonQuestions",
                columns: new[] { "StudentUserId", "LastMessageAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFCourseInstructors");

            migrationBuilder.DropTable(
                name: "LFLessonQuestionMessages");

            migrationBuilder.DropTable(
                name: "LFLessonQuestionReadMarkers");

            migrationBuilder.DropTable(
                name: "LFLessonQuestions");
        }
    }
}
