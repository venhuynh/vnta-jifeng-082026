using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260720050000_DropObsoleteMealAllowanceSummaryReference")]
public partial class DropObsoleteMealAllowanceSummaryReference : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Phụ cấp cơm là snapshot độc lập và đồng bộ summary theo EmployeeId + kỳ lương.
        // Cột legacy này không thuộc model EF/runtime hiện hành, nên phải loại bỏ để tránh hai nguồn định danh.
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF to_regclass('public.payroll_allowance_meal_records') IS NOT NULL THEN
                    ALTER TABLE public.payroll_allowance_meal_records
                    DROP COLUMN IF EXISTS "PayrollAllowanceSummaryRecordId";
                ELSIF to_regclass('public.payroll_meal_allowance_records') IS NOT NULL THEN
                    ALTER TABLE public.payroll_meal_allowance_records
                    DROP COLUMN IF EXISTS "PayrollAllowanceSummaryRecordId";
                END IF;
            END $$;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF to_regclass('public.payroll_allowance_meal_records') IS NOT NULL THEN
                    ALTER TABLE public.payroll_allowance_meal_records
                    ADD COLUMN IF NOT EXISTS "PayrollAllowanceSummaryRecordId" uuid NULL;
                ELSIF to_regclass('public.payroll_meal_allowance_records') IS NOT NULL THEN
                    ALTER TABLE public.payroll_meal_allowance_records
                    ADD COLUMN IF NOT EXISTS "PayrollAllowanceSummaryRecordId" uuid NULL;
                END IF;
            END $$;
            """);
    }
}
