using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestorePayrollResponsibilityAllowanceAbcSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                type: "uuid",
                nullable: true);

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

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM payroll_allowance_responsibility_abc AS abc
                        LEFT JOIN payroll_allowance_summary_records AS summary
                            ON summary."Id" = abc."PayrollAllowanceSummaryRecordId"
                        LEFT JOIN employees AS employee
                            ON employee."Id" = summary."EmployeeId"
                        WHERE summary."Id" IS NULL OR employee."Id" IS NULL
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot restore responsibility ABC snapshots: a row has no valid payroll summary or employee.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM payroll_allowance_responsibility_abc AS abc
                        INNER JOIN payroll_allowance_summary_records AS summary
                            ON summary."Id" = abc."PayrollAllowanceSummaryRecordId"
                        GROUP BY abc."Year", abc."Month", summary."EmployeeId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot restore responsibility ABC snapshots: duplicate employee-period identity was detected.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                UPDATE payroll_allowance_responsibility_abc AS abc
                SET
                    "EmployeeId" = summary."EmployeeId",
                    "EmployeeCode" = employee."EmployeeCode",
                    "EmployeeName" = btrim(concat_ws(' ', NULLIF(btrim(employee."LastName"), ''), NULLIF(btrim(employee."FirstName"), ''))),
                    "DepartmentName" = COALESCE(
                        NULLIF(btrim(department."GroupName"), ''),
                        NULLIF(btrim(department."TeamName"), ''),
                        NULLIF(btrim(department."DepartmentOrWorkshopName"), ''),
                        ''),
                    "PositionId" = employee."PositionId",
                    "PositionName" = COALESCE(position."Name", '')
                FROM payroll_allowance_summary_records AS summary
                INNER JOIN employees AS employee
                    ON employee."Id" = summary."EmployeeId"
                LEFT JOIN departments AS department
                    ON department."Id" = employee."DepartmentId"
                LEFT JOIN positions AS position
                    ON position."Id" = employee."PositionId"
                WHERE abc."PayrollAllowanceSummaryRecordId" = summary."Id";
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM payroll_allowance_responsibility_abc
                        WHERE "EmployeeId" IS NULL
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot restore responsibility ABC snapshots: employee backfill was incomplete.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_responsibility_abc_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UX_payroll_allowance_responsibility_abc_Year_Month_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                columns: new[] { "Year", "Month", "EmployeeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_employees_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_employees_EmployeeId",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropIndex(
                name: "IX_payroll_allowance_responsibility_abc_EmployeeId",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropIndex(
                name: "UX_payroll_allowance_responsibility_abc_Year_Month_EmployeeId",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "EmployeeName",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropColumn(
                name: "PositionName",
                table: "payroll_allowance_responsibility_abc");
        }
    }
}
