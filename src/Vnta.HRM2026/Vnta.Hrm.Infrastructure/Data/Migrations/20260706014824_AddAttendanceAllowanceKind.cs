using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAllowanceKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendance_allowance_results_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results");

            migrationBuilder.DropIndex(
                name: "UX_attendance_allowance_results_EmployeeId_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results");

            migrationBuilder.AddColumn<short>(
                name: "AllowanceKind",
                table: "attendance_allowance_results",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_allowance_results_AllowanceKind_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results",
                columns: new[] { "AllowanceKind", "PayrollYear", "PayrollMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_allowance_results_EmployeeId",
                table: "attendance_allowance_results",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UX_attendance_allowance_results_AllowanceKind_EmployeeId_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results",
                columns: new[] { "AllowanceKind", "EmployeeId", "PayrollYear", "PayrollMonth" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_allowance_results_AllowanceKind",
                table: "attendance_allowance_results",
                sql: "\"AllowanceKind\" IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendance_allowance_results_AllowanceKind_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results");

            migrationBuilder.DropIndex(
                name: "IX_attendance_allowance_results_EmployeeId",
                table: "attendance_allowance_results");

            migrationBuilder.DropIndex(
                name: "UX_attendance_allowance_results_AllowanceKind_EmployeeId_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attendance_allowance_results_AllowanceKind",
                table: "attendance_allowance_results");

            migrationBuilder.DropColumn(
                name: "AllowanceKind",
                table: "attendance_allowance_results");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_allowance_results_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results",
                columns: new[] { "PayrollYear", "PayrollMonth" });

            migrationBuilder.CreateIndex(
                name: "UX_attendance_allowance_results_EmployeeId_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results",
                columns: new[] { "EmployeeId", "PayrollYear", "PayrollMonth" },
                unique: true);
        }
    }
}
