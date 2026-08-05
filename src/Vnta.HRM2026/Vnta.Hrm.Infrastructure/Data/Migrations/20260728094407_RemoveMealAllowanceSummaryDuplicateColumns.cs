using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMealAllowanceSummaryDuplicateColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_meal_records_employees_EmployeeId",
                table: "payroll_allowance_meal_records");

            migrationBuilder.DropIndex(
                name: "IX_payroll_allowance_meal_records_PayrollYear_PayrollMonth",
                table: "payroll_allowance_meal_records");

            migrationBuilder.DropIndex(
                name: "UX_payroll_allowance_meal_records_EmployeeId_PayrollYear_PayrollMonth",
                table: "payroll_allowance_meal_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_meal_records_PayrollMonth",
                table: "payroll_allowance_meal_records");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "payroll_allowance_meal_records");

            migrationBuilder.DropColumn(
                name: "PayrollMonth",
                table: "payroll_allowance_meal_records");

            migrationBuilder.DropColumn(
                name: "PayrollYear",
                table: "payroll_allowance_meal_records");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "payroll_allowance_meal_records",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<short>(
                name: "PayrollMonth",
                table: "payroll_allowance_meal_records",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "PayrollYear",
                table: "payroll_allowance_meal_records",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.Sql(
                """
                UPDATE public.payroll_allowance_meal_records AS meal
                SET
                    "EmployeeId" = summary."EmployeeId",
                    "PayrollMonth" = summary."PayrollMonth",
                    "PayrollYear" = summary."PayrollYear"
                FROM public.payroll_allowance_summary_records AS summary
                WHERE summary."Id" = meal."PayrollAllowanceSummaryRecordId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_meal_records_PayrollYear_PayrollMonth",
                table: "payroll_allowance_meal_records",
                columns: new[] { "PayrollYear", "PayrollMonth" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_allowance_meal_records_EmployeeId_PayrollYear_PayrollMonth",
                table: "payroll_allowance_meal_records",
                columns: new[] { "EmployeeId", "PayrollYear", "PayrollMonth" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_meal_records_PayrollMonth",
                table: "payroll_allowance_meal_records",
                sql: "\"PayrollMonth\" >= 1 AND \"PayrollMonth\" <= 12");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_meal_records_employees_EmployeeId",
                table: "payroll_allowance_meal_records",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
