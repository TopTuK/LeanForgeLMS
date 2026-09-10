using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropPlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFPlatformSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LFPlatformSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    StudentEnrollmentEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFPlatformSettings", x => x.Id);
                });
        }
    }
}
