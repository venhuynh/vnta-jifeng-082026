using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAttendanceWorkdaySummaryStatusWithStatusCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CodeKetQuaTinhCongId",
                table: "attendance_workday_summaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE attendance_workday_summaries AS summary
                SET "CodeKetQuaTinhCongId" = status_code."Id"
                FROM attendance_status_codes AS status_code
                WHERE summary."Status" = status_code."Code";
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "attendance_workday_summaries");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_workday_summaries_CodeKetQuaTinhCongId",
                table: "attendance_workday_summaries",
                column: "CodeKetQuaTinhCongId");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_workday_summaries_attendance_status_codes_CodeKe~",
                table: "attendance_workday_summaries",
                column: "CodeKetQuaTinhCongId",
                principalTable: "attendance_status_codes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendance_workday_summaries_attendance_status_codes_CodeKe~",
                table: "attendance_workday_summaries");

            migrationBuilder.DropIndex(
                name: "IX_attendance_workday_summaries_CodeKetQuaTinhCongId",
                table: "attendance_workday_summaries");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "attendance_workday_summaries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE attendance_workday_summaries AS summary
                SET "Status" = status_code."Code"
                FROM attendance_status_codes AS status_code
                WHERE summary."CodeKetQuaTinhCongId" = status_code."Id";
                """);

            migrationBuilder.DropColumn(
                name: "CodeKetQuaTinhCongId",
                table: "attendance_workday_summaries");
        }
    }
}
