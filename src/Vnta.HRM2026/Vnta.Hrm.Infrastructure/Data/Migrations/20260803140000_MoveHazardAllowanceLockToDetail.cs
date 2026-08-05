using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Moves the hazard-allowance lock from the shared payroll summary to the
/// feature-owned hazard detail record.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260803140000_MoveHazardAllowanceLockToDetail")]
public partial class MoveHazardAllowanceLockToDetail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsLocked",
            table: "payroll_allowance_hazard_records",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_payroll_allowance_hazard_records_IsLocked",
            table: "payroll_allowance_hazard_records",
            column: "IsLocked");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_payroll_allowance_hazard_records_IsLocked",
            table: "payroll_allowance_hazard_records");

        migrationBuilder.DropColumn(
            name: "IsLocked",
            table: "payroll_allowance_hazard_records");
    }
}
