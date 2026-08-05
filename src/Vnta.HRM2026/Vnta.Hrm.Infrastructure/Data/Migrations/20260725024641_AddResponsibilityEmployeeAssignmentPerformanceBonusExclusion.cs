using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddResponsibilityEmployeeAssignmentPerformanceBonusExclusion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsPerformanceBonusExcluded",
            table: "payroll_allowance_responsibility_employee_assignments",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE payroll_allowance_responsibility_employee_assignments AS assignment
            SET "IsPerformanceBonusExcluded" = TRUE
            FROM payroll_allowance_responsibility_abc AS abc
            WHERE abc."EmployeeId" = assignment."EmployeeId"
              AND abc."Year" = assignment."Year"
              AND abc."Month" = assignment."Month"
              AND abc."IsPerformanceBonusExcluded" = TRUE;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsPerformanceBonusExcluded",
            table: "payroll_allowance_responsibility_employee_assignments");
    }
}
