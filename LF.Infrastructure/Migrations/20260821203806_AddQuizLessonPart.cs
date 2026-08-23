using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizLessonPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuizPassThresholdPercent",
                table: "LFLessonParts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LFQuizAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    ScorePercent = table.Column<int>(type: "integer", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFQuizAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFQuizAttempts_LFEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "LFEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFQuizQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: false),
                    QuestionType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    LessonPartId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFQuizQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFQuizQuestions_LFLessonParts_LessonPartId",
                        column: x => x.LessonPartId,
                        principalTable: "LFLessonParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFQuizAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizAttemptId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    SelectedOptionIds = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFQuizAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFQuizAnswers_LFQuizAttempts_QuizAttemptId",
                        column: x => x.QuizAttemptId,
                        principalTable: "LFQuizAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFQuizOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFQuizOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFQuizOptions_LFQuizQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "LFQuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFQuizAnswers_QuizAttemptId",
                table: "LFQuizAnswers",
                column: "QuizAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_LFQuizAttempts_EnrollmentId",
                table: "LFQuizAttempts",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LFQuizOptions_QuestionId",
                table: "LFQuizOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_LFQuizQuestions_LessonPartId",
                table: "LFQuizQuestions",
                column: "LessonPartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFQuizAnswers");

            migrationBuilder.DropTable(
                name: "LFQuizOptions");

            migrationBuilder.DropTable(
                name: "LFQuizAttempts");

            migrationBuilder.DropTable(
                name: "LFQuizQuestions");

            migrationBuilder.DropColumn(
                name: "QuizPassThresholdPercent",
                table: "LFLessonParts");
        }
    }
}
