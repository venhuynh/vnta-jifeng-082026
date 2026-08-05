using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Creates the leave/holiday detail snapshot for every pre-existing allowance summary.
/// New detail rows retain the historical summary amount until an explicit recalculation.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731100000_BackfillLeaveHolidayAllowanceDetailSnapshots")]
public partial class BackfillLeaveHolidayAllowanceDetailSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO public.payroll_allowance_summary_leave_holiday_records (
                "PayrollAllowanceSummaryRecordId",
                "DailyWageAmount",
                "LeaveDayCount",
                "HolidayDayCount",
                "LeaveHolidayAllowanceAmount",
                "Note",
                "CreatedAtUtc",
                "CreatedBy",
                "UpdatedAtUtc",
                "UpdatedBy")
            SELECT
                summary."Id",
                0,
                0,
                0,
                summary."LeaveHolidayAllowanceAmount",
                NULL,
                CURRENT_TIMESTAMP,
                'system-backfill',
                NULL,
                NULL
            FROM public.payroll_allowance_summary_records AS summary
            WHERE NOT EXISTS (
                SELECT 1
                FROM public.payroll_allowance_summary_leave_holiday_records AS detail
                WHERE detail."PayrollAllowanceSummaryRecordId" = summary."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The backfilled snapshots become operational data and must not be removed on rollback.
    }
}
