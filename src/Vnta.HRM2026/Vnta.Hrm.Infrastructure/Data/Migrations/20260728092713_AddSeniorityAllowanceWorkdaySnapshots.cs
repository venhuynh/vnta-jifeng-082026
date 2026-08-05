using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeniorityAllowanceWorkdaySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_SalaryWorkDays",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.AlterColumn<decimal>(
                name: "SalaryWorkDays",
                table: "payroll_allowance_seniority_records",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,2)",
                oldPrecision: 9,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdministrativeWorkDays",
                table: "payroll_allowance_seniority_records",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LateEarlyLeaveWorkDays",
                table: "payroll_allowance_seniority_records",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_AdministrativeWorkDays",
                table: "payroll_allowance_seniority_records",
                sql: "\"AdministrativeWorkDays\" IS NULL OR \"AdministrativeWorkDays\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_LateEarlyLeaveWorkDays",
                table: "payroll_allowance_seniority_records",
                sql: "\"LateEarlyLeaveWorkDays\" IS NULL OR \"LateEarlyLeaveWorkDays\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_AdministrativeWorkDays",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_LateEarlyLeaveWorkDays",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.DropColumn(
                name: "AdministrativeWorkDays",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.DropColumn(
                name: "LateEarlyLeaveWorkDays",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.AlterColumn<decimal>(
                name: "SalaryWorkDays",
                table: "payroll_allowance_seniority_records",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_SalaryWorkDays",
                table: "payroll_allowance_seniority_records",
                sql: "\"SalaryWorkDays\" IS NULL OR \"SalaryWorkDays\" >= 0");
        }
    }
}
