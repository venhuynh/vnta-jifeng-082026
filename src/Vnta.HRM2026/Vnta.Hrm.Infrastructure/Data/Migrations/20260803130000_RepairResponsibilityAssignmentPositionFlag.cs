using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Repairs databases whose migration history records the responsibility-assignment
/// refactor but whose physical table is missing the position-default flag.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260803130000_RepairResponsibilityAssignmentPositionFlag")]
public partial class RepairResponsibilityAssignmentPositionFlag : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.payroll_allowance_responsibility_employee_assignments
                ADD COLUMN IF NOT EXISTS "IsAssignGradeFromPosition" boolean NOT NULL DEFAULT TRUE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This is a schema-repair migration. Retain the column on rollback so
        // an already-repaired database remains compatible with the application model.
    }
}
