using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260730110000_ReconcileMealAllowanceSummaryProjection")]
public partial class ReconcileMealAllowanceSummaryProjection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The detail record is canonical. Reconcile only summaries that have a detail
        // record and differ from it; no synthetic detail data is introduced here.
        migrationBuilder.Sql(
            """
            UPDATE public.payroll_allowance_summary_records AS summary
            SET "MealAllowanceAmount" = meal."MealAllowanceAmount"
            FROM public.payroll_allowance_meal_records AS meal
            WHERE meal."PayrollAllowanceSummaryRecordId" = summary."Id"
              AND summary."MealAllowanceAmount" IS DISTINCT FROM meal."MealAllowanceAmount";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The prior non-canonical amount cannot be reconstructed safely.
    }
}
