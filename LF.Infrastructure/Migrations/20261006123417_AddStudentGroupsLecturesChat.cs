using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentGroupsLecturesChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LFLectures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MeetingUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFLectures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFLectures_LFCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "LFCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFStudentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFStudentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFStudentGroups_LFCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "LFCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFGroupChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFGroupChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFGroupChatMessages_LFStudentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "LFStudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFGroupChatReadMarkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LastSeenMessageId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFGroupChatReadMarkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFGroupChatReadMarkers_LFStudentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "LFStudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFLectureGroups",
                columns: table => new
                {
                    LectureId = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFLectureGroups", x => new { x.LectureId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_LFLectureGroups_LFLectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "LFLectures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LFLectureGroups_LFStudentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "LFStudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LFStudentGroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AddedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFStudentGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFStudentGroupMembers_LFStudentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "LFStudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFGroupChatMessages_GroupId_Id",
                table: "LFGroupChatMessages",
                columns: new[] { "GroupId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LFGroupChatReadMarkers_GroupId_UserId",
                table: "LFGroupChatReadMarkers",
                columns: new[] { "GroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LFGroupChatReadMarkers_UserId",
                table: "LFGroupChatReadMarkers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LFLectureGroups_GroupId",
                table: "LFLectureGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_LFLectures_CourseId_StartsAt",
                table: "LFLectures",
                columns: new[] { "CourseId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LFStudentGroupMembers_GroupId_UserId",
                table: "LFStudentGroupMembers",
                columns: new[] { "GroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LFStudentGroupMembers_UserId",
                table: "LFStudentGroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LFStudentGroups_CourseId_Name",
                table: "LFStudentGroups",
                columns: new[] { "CourseId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFGroupChatMessages");

            migrationBuilder.DropTable(
                name: "LFGroupChatReadMarkers");

            migrationBuilder.DropTable(
                name: "LFLectureGroups");

            migrationBuilder.DropTable(
                name: "LFStudentGroupMembers");

            migrationBuilder.DropTable(
                name: "LFLectures");

            migrationBuilder.DropTable(
                name: "LFStudentGroups");
        }
    }
}
