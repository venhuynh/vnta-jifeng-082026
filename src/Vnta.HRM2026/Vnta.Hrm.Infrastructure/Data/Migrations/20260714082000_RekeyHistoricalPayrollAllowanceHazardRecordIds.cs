using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260714082000_RekeyHistoricalPayrollAllowanceHazardRecordIds")]
    public sealed class RekeyHistoricalPayrollAllowanceHazardRecordIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH remapped AS (
                    SELECT
                        "PayrollAllowanceSummaryRecordId",
                        (
                            substr(md5("PayrollAllowanceSummaryRecordId"::text || ':payroll_allowance_hazard_records'), 1, 8) || '-' ||
                            substr(md5("PayrollAllowanceSummaryRecordId"::text || ':payroll_allowance_hazard_records'), 9, 4) || '-' ||
                            substr(md5("PayrollAllowanceSummaryRecordId"::text || ':payroll_allowance_hazard_records'), 13, 4) || '-' ||
                            substr(md5("PayrollAllowanceSummaryRecordId"::text || ':payroll_allowance_hazard_records'), 17, 4) || '-' ||
                            substr(md5("PayrollAllowanceSummaryRecordId"::text || ':payroll_allowance_hazard_records'), 21, 12)
                        )::uuid AS "NewId"
                    FROM payroll_allowance_hazard_records
                    WHERE "Id" = "PayrollAllowanceSummaryRecordId"
                )
                UPDATE payroll_allowance_hazard_records AS target
                SET "Id" = remapped."NewId"
                FROM remapped
                WHERE target."PayrollAllowanceSummaryRecordId" = remapped."PayrollAllowanceSummaryRecordId";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE payroll_allowance_hazard_records
                SET "Id" = "PayrollAllowanceSummaryRecordId"
                WHERE "Id" <> "PayrollAllowanceSummaryRecordId";
                """);
        }

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
        }
    }
}
