using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyPayrollResponsibilityAllowanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_employees_EmployeeId",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public.payroll_allowance_responsibility_records') IS NOT NULL THEN
                        DROP TABLE public.payroll_allowance_responsibility_records;
                    ELSIF to_regclass('public.payroll_employee_responsibility_allowances') IS NOT NULL THEN
                        DROP TABLE public.payroll_employee_responsibility_allowances;
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_payroll_allowance_responsibility_abc_Year_Month_DepartmentName",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropIndex(
                name: "IX_payroll_allowance_responsibility_abc_Year_Month_EmployeeCode",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "EmployeeName",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "PositionName",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                newName: "PayrollAllowanceSummaryRecordId");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_allowance_responsibility_abc_Year_Month_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                newName: "UX_payroll_allowance_responsibility_abc_Year_Month_PayrollAllowanceSummaryRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_abc_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                newName: "IX_payroll_allowance_responsibility_abc_PayrollAllowanceSummar~");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_payroll_allowance_summ~",
                table: "payroll_allowance_responsibility_abc",
                column: "PayrollAllowanceSummaryRecordId",
                principalTable: "payroll_allowance_summary_records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_payroll_allowance_summ~",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.RenameColumn(
                name: "PayrollAllowanceSummaryRecordId",
                table: "payroll_allowance_responsibility_abc",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_allowance_responsibility_abc_Year_Month_PayrollAllowanceSummaryRecordId",
                table: "payroll_allowance_responsibility_abc",
                newName: "UX_payroll_allowance_responsibility_abc_Year_Month_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_abc_PayrollAllowanceSummar~",
                table: "payroll_allowance_responsibility_abc",
                newName: "IX_payroll_allowance_responsibility_abc_EmployeeId");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "payroll_allowance_responsibility_abc",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "payroll_allowance_responsibility_abc",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeName",
                table: "payroll_allowance_responsibility_abc",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PositionName",
                table: "payroll_allowance_responsibility_abc",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "payroll_allowance_responsibility_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EmployeeCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PositionName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_allowance_responsibility_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_allowance_responsibility_records_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_responsibility_abc_Year_Month_DepartmentName",
                table: "payroll_allowance_responsibility_abc",
                columns: new[] { "Year", "Month", "DepartmentName" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_responsibility_abc_Year_Month_EmployeeCode",
                table: "payroll_allowance_responsibility_abc",
                columns: new[] { "Year", "Month", "EmployeeCode" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_responsibility_records_EmployeeId",
                table: "payroll_allowance_responsibility_records",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_responsibility_records_Year_Month",
                table: "payroll_allowance_responsibility_records",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_responsibility_records_Year_Month_EmployeeCode",
                table: "payroll_allowance_responsibility_records",
                columns: new[] { "Year", "Month", "EmployeeCode" });

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_employees_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
