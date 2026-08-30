using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursePricingAndPromoCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePaid",
                table: "LFEnrollments",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PromoCodeId",
                table: "LFEnrollments",
                type: "integer",
                nullable: true);

            // Existing enrollments predate paid courses — they are all active. EnrollmentStatus.Active = 1.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "LFEnrollments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Existing courses predate managed enrollment — they are all open. CourseEnrollmentMode.Open = 1.
            migrationBuilder.AddColumn<int>(
                name: "EnrollmentMode",
                table: "LFCourses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "LFCourses",
                type: "numeric(12,2)",
                nullable: true);

            // Existing courses predate paid courses — they are all free. CoursePricingType.Free = 1.
            migrationBuilder.AddColumn<int>(
                name: "PricingType",
                table: "LFCourses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "LFPromoCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    RedemptionCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFPromoCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LFPromoCodes_LFCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "LFCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LFEnrollments_PromoCodeId",
                table: "LFEnrollments",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LFPromoCodes_Code",
                table: "LFPromoCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LFPromoCodes_CourseId",
                table: "LFPromoCodes",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_LFEnrollments_LFPromoCodes_PromoCodeId",
                table: "LFEnrollments",
                column: "PromoCodeId",
                principalTable: "LFPromoCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LFEnrollments_LFPromoCodes_PromoCodeId",
                table: "LFEnrollments");

            migrationBuilder.DropTable(
                name: "LFPromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_LFEnrollments_PromoCodeId",
                table: "LFEnrollments");

            migrationBuilder.DropColumn(
                name: "PricePaid",
                table: "LFEnrollments");

            migrationBuilder.DropColumn(
                name: "PromoCodeId",
                table: "LFEnrollments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LFEnrollments");

            migrationBuilder.DropColumn(
                name: "EnrollmentMode",
                table: "LFCourses");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "LFCourses");

            migrationBuilder.DropColumn(
                name: "PricingType",
                table: "LFCourses");
        }
    }
}
