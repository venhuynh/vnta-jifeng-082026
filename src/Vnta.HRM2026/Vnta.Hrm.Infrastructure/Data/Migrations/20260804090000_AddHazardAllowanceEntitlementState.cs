using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>Stores the user-controlled entitlement state independently from the calculated department check.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804090000_AddHazardAllowanceEntitlementState")]
public partial class AddHazardAllowanceEntitlementState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsEligibleForAllowance",
            table: "payroll_allowance_hazard_records",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("""
            UPDATE payroll_allowance_hazard_records
            SET "IsEligibleForAllowance" = "IsEligibleDepartment";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsEligibleForAllowance",
            table: "payroll_allowance_hazard_records");
    }
}
