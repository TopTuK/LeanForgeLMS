using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LF.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSettingsAndCoursePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LFCoursePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentOrderId = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    StudentEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StudentName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    CourseTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PromoCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderOperationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LFCoursePayments", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_LFCoursePayments_CourseId",
                table: "LFCoursePayments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LFCoursePayments_EnrollmentId",
                table: "LFCoursePayments",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LFCoursePayments_PaidAt",
                table: "LFCoursePayments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_LFCoursePayments_PaymentOrderId",
                table: "LFCoursePayments",
                column: "PaymentOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LFCoursePayments_UserId",
                table: "LFCoursePayments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LFCoursePayments");

            migrationBuilder.DropTable(
                name: "LFPlatformSettings");
        }
    }
}
