using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Liên kết assignment trách nhiệm một-một với Summary để kỳ lương và nhân viên
/// luôn lấy từ nguồn dữ liệu tổng hợp duy nhất.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260729120000_RefactorResponsibilityEmployeeAssignmentsToSummaryLink")]
public partial class RefactorResponsibilityEmployeeAssignmentsToSummaryLink : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE payroll_allowance_responsibility_employee_assignments
                ADD COLUMN "PayrollAllowanceSummaryRecordId" uuid NULL,
                ADD COLUMN "IsAssignGradeFromPosition" boolean NOT NULL DEFAULT FALSE;

            UPDATE payroll_allowance_responsibility_employee_assignments AS assignment
            SET "PayrollAllowanceSummaryRecordId" = summary."Id",
                "IsAssignGradeFromPosition" = assignment."AssignmentSource" = 'position-default'
            FROM payroll_allowance_summary_records AS summary
            WHERE summary."EmployeeId" = assignment."EmployeeId"
              AND summary."PayrollYear" = assignment."Year"
              AND summary."PayrollMonth" = assignment."Month";

            DELETE FROM payroll_allowance_responsibility_employee_assignments
            WHERE "PayrollAllowanceSummaryRecordId" IS NULL;

            DROP INDEX IF EXISTS "IX_payroll_allowance_responsibility_employee_assignments_GradeId";

            ALTER TABLE payroll_allowance_responsibility_employee_assignments
                DROP CONSTRAINT IF EXISTS "CK_payroll_allowance_responsibility_employee_assignments_Month",
                DROP CONSTRAINT IF EXISTS "CK_payroll_allowance_responsibility_employee_assignments_StandardResponsibilityAllowanceAmount",
                DROP CONSTRAINT IF EXISTS "FK_payroll_allowance_responsibility_employee_assignments_employees_EmployeeId",
                DROP CONSTRAINT IF EXISTS "FK_payroll_allowance_responsibility_employee_assignments_payroll_allowance_responsibility_grade_GradeId",
                DROP COLUMN "Year",
                DROP COLUMN "Month",
                DROP COLUMN "EmployeeId",
                DROP COLUMN "StandardResponsibilityAllowanceAmount",
                DROP COLUMN "AssignmentSource",
                ALTER COLUMN "PayrollAllowanceSummaryRecordId" SET NOT NULL,
                ALTER COLUMN "GradeId" DROP NOT NULL;

            CREATE UNIQUE INDEX "UX_payroll_allowance_responsibility_employee_assignments_PayrollAllowanceSummaryRecordId"
                ON payroll_allowance_responsibility_employee_assignments ("PayrollAllowanceSummaryRecordId");
            CREATE INDEX "IX_payroll_allowance_responsibility_employee_assignments_GradeId"
                ON payroll_allowance_responsibility_employee_assignments ("GradeId");
            ALTER TABLE payroll_allowance_responsibility_employee_assignments
                ADD CONSTRAINT "FK_payroll_allowance_responsibility_employee_assignments_summary"
                    FOREIGN KEY ("PayrollAllowanceSummaryRecordId")
                    REFERENCES payroll_allowance_summary_records ("Id") ON DELETE RESTRICT,
                ADD CONSTRAINT "FK_payroll_allowance_responsibility_employee_assignments_grade"
                    FOREIGN KEY ("GradeId")
                    REFERENCES payroll_allowance_responsibility_grade ("Id") ON DELETE RESTRICT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE payroll_allowance_responsibility_employee_assignments
                ADD COLUMN "Year" integer NULL,
                ADD COLUMN "Month" integer NULL,
                ADD COLUMN "EmployeeId" uuid NULL,
                ADD COLUMN "StandardResponsibilityAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                ADD COLUMN "AssignmentSource" character varying(50) NOT NULL DEFAULT 'employee-assignment';

            UPDATE payroll_allowance_responsibility_employee_assignments AS assignment
            SET "Year" = summary."PayrollYear",
                "Month" = summary."PayrollMonth",
                "EmployeeId" = summary."EmployeeId",
                "StandardResponsibilityAllowanceAmount" = COALESCE(grade."StandardResponsibilityAllowanceAmount", 0),
                "AssignmentSource" = CASE
                    WHEN assignment."IsAssignGradeFromPosition" THEN 'position-default'
                    ELSE 'employee-assignment'
                END
            FROM payroll_allowance_summary_records AS summary
            LEFT JOIN payroll_allowance_responsibility_grade AS grade ON grade."Id" = assignment."GradeId"
            WHERE summary."Id" = assignment."PayrollAllowanceSummaryRecordId";

            DELETE FROM payroll_allowance_responsibility_employee_assignments
            WHERE "GradeId" IS NULL;

            DROP INDEX IF EXISTS "IX_payroll_allowance_responsibility_employee_assignments_GradeId";
            ALTER TABLE payroll_allowance_responsibility_employee_assignments
                DROP CONSTRAINT IF EXISTS "FK_payroll_allowance_responsibility_employee_assignments_summary",
                DROP CONSTRAINT IF EXISTS "FK_payroll_allowance_responsibility_employee_assignments_grade",
                DROP COLUMN "PayrollAllowanceSummaryRecordId",
                DROP COLUMN "IsAssignGradeFromPosition",
                ALTER COLUMN "Year" SET NOT NULL,
                ALTER COLUMN "Month" SET NOT NULL,
                ALTER COLUMN "EmployeeId" SET NOT NULL,
                ALTER COLUMN "GradeId" SET NOT NULL;

            ALTER TABLE payroll_allowance_responsibility_employee_assignments
                ADD CONSTRAINT "CK_payroll_allowance_responsibility_employee_assignments_Month"
                    CHECK ("Month" BETWEEN 1 AND 12),
                ADD CONSTRAINT "CK_payroll_allowance_responsibility_employee_assignments_StandardResponsibilityAllowanceAmount"
                    CHECK ("StandardResponsibilityAllowanceAmount" >= 0),
                ADD CONSTRAINT "FK_payroll_allowance_responsibility_employee_assignments_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES employees ("Id") ON DELETE RESTRICT,
                ADD CONSTRAINT "FK_payroll_allowance_responsibility_employee_assignments_payroll_allowance_responsibility_grade_GradeId"
                    FOREIGN KEY ("GradeId") REFERENCES payroll_allowance_responsibility_grade ("Id") ON DELETE RESTRICT;
            CREATE UNIQUE INDEX "UX_payroll_allowance_responsibility_employee_assignments_Year_Month_EmployeeId"
                ON payroll_allowance_responsibility_employee_assignments ("Year", "Month", "EmployeeId");
            CREATE INDEX "IX_payroll_allowance_responsibility_employee_assignments_Year_Month_GradeId"
                ON payroll_allowance_responsibility_employee_assignments ("Year", "Month", "GradeId");
            CREATE INDEX "IX_payroll_allowance_responsibility_employee_assignments_GradeId"
                ON payroll_allowance_responsibility_employee_assignments ("GradeId");
            """);
    }
}
