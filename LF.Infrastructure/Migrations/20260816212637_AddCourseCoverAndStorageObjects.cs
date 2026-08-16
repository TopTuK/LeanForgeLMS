using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCoverAndStorageObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageKey",
                table: "LFCourses");

            migrationBuilder.AddColumn<int>(
                name: "CoverColor",
                table: "LFCourses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverImageStorageObjectId",
                table: "LFCourses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverType",
                table: "LFCourses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LFStorageObjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObjectType = table.Column<int>(type: "integer", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFStorageObjects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFCourses_CoverImageStorageObjectId",
                table: "LFCourses",
                column: "CoverImageStorageObjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_LFCourses_LFStorageObjects_CoverImageStorageObjectId",
                table: "LFCourses",
                column: "CoverImageStorageObjectId",
                principalTable: "LFStorageObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LFCourses_LFStorageObjects_CoverImageStorageObjectId",
                table: "LFCourses");

            migrationBuilder.DropTable(
                name: "LFStorageObjects");

            migrationBuilder.DropIndex(
                name: "IX_LFCourses_CoverImageStorageObjectId",
                table: "LFCourses");

            migrationBuilder.DropColumn(
                name: "CoverColor",
                table: "LFCourses");

            migrationBuilder.DropColumn(
                name: "CoverImageStorageObjectId",
                table: "LFCourses");

            migrationBuilder.DropColumn(
                name: "CoverType",
                table: "LFCourses");

            migrationBuilder.AddColumn<string>(
                name: "ImageKey",
                table: "LFCourses",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);
        }
    }
}
