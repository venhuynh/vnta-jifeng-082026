using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameAttendanceAllowanceResultsToPayrollAllowanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendance_allowance_results_employees_EmployeeId",
                table: "attendance_allowance_results");

            migrationBuilder.DropPrimaryKey(
                name: "PK_attendance_allowance_results",
                table: "attendance_allowance_results");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attendance_allowance_results_ActualWorkdayCount",
                table: "attendance_allowance_results");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attendance_allowance_results_AllowanceKind",
                table: "attendance_allowance_results");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attendance_allowance_results_AttendanceRate",
                table: "attendance_allowance_results");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attendance_allowance_results_PayrollMonth",
                table: "attendance_allowance_results");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attendance_allowance_results_StandardWorkdayCount",
                table: "attendance_allowance_results");

            migrationBuilder.RenameTable(
                name: "attendance_allowance_results",
                newName: "payroll_allowance_records");

            migrationBuilder.RenameIndex(
                name: "UX_attendance_allowance_results_AllowanceKind_EmployeeId_PayrollYear_PayrollMonth",
                table: "payroll_allowance_records",
                newName: "UX_payroll_allowance_records_AllowanceKind_EmployeeId_PayrollYear_PayrollMonth");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_allowance_results_IsLocked",
                table: "payroll_allowance_records",
                newName: "IX_payroll_allowance_records_IsLocked");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_allowance_results_EmployeeId",
                table: "payroll_allowance_records",
                newName: "IX_payroll_allowance_records_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_allowance_results_AllowanceKind_PayrollYear_PayrollMonth",
                table: "payroll_allowance_records",
                newName: "IX_payroll_allowance_records_AllowanceKind_PayrollYear_PayrollMonth");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_allowance_records",
                table: "payroll_allowance_records",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_records_ActualWorkdayCount",
                table: "payroll_allowance_records",
                sql: "\"ActualWorkdayCount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_records_AllowanceKind",
                table: "payroll_allowance_records",
                sql: "\"AllowanceKind\" IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_records_AttendanceRate",
                table: "payroll_allowance_records",
                sql: "\"AttendanceRate\" >= 0 AND \"AttendanceRate\" <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_records_PayrollMonth",
                table: "payroll_allowance_records",
                sql: "\"PayrollMonth\" >= 1 AND \"PayrollMonth\" <= 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_records_StandardWorkdayCount",
                table: "payroll_allowance_records",
                sql: "\"StandardWorkdayCount\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_records_employees_EmployeeId",
                table: "payroll_allowance_records",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_records_employees_EmployeeId",
                table: "payroll_allowance_records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_allowance_records",
                table: "payroll_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_records_ActualWorkdayCount",
                table: "payroll_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_records_AllowanceKind",
                table: "payroll_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_records_AttendanceRate",
                table: "payroll_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_records_PayrollMonth",
                table: "payroll_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_records_StandardWorkdayCount",
                table: "payroll_allowance_records");

            migrationBuilder.RenameTable(
                name: "payroll_allowance_records",
                newName: "attendance_allowance_results");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_allowance_records_AllowanceKind_EmployeeId_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results",
                newName: "UX_attendance_allowance_results_AllowanceKind_EmployeeId_PayrollYear_PayrollMonth");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_records_IsLocked",
                table: "attendance_allowance_results",
                newName: "IX_attendance_allowance_results_IsLocked");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_records_EmployeeId",
                table: "attendance_allowance_results",
                newName: "IX_attendance_allowance_results_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_records_AllowanceKind_PayrollYear_PayrollMonth",
                table: "attendance_allowance_results",
                newName: "IX_attendance_allowance_results_AllowanceKind_PayrollYear_PayrollMonth");

            migrationBuilder.AddPrimaryKey(
                name: "PK_attendance_allowance_results",
                table: "attendance_allowance_results",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_allowance_results_ActualWorkdayCount",
                table: "attendance_allowance_results",
                sql: "\"ActualWorkdayCount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_allowance_results_AllowanceKind",
                table: "attendance_allowance_results",
                sql: "\"AllowanceKind\" IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_allowance_results_AttendanceRate",
                table: "attendance_allowance_results",
                sql: "\"AttendanceRate\" >= 0 AND \"AttendanceRate\" <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_allowance_results_PayrollMonth",
                table: "attendance_allowance_results",
                sql: "\"PayrollMonth\" >= 1 AND \"PayrollMonth\" <= 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_allowance_results_StandardWorkdayCount",
                table: "attendance_allowance_results",
                sql: "\"StandardWorkdayCount\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_allowance_results_employees_EmployeeId",
                table: "attendance_allowance_results",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
