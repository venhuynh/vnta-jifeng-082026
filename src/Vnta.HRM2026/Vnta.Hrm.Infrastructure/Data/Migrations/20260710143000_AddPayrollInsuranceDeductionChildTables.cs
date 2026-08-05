using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollInsuranceDeductionChildTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.payroll_decuction_summary_insurance_details (
                    "Id" uuid NOT NULL,
                    "PayrollDeductionSummaryRecordId" uuid NOT NULL,
                    "StandardAllowanceAmount" numeric(18,2) NOT NULL,
                    "StandardWorkdayCount" numeric(10,2) NOT NULL,
                    "ActualWorkdayCount" numeric(10,2) NOT NULL,
                    "AttendanceRate" numeric(7,4) NOT NULL,
                    "ActualAllowanceAmount" numeric(18,2) NOT NULL,
                    "IsLocked" boolean NOT NULL DEFAULT FALSE,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone NULL,
                    CONSTRAINT "PK_payroll_decuction_summary_insurance_details" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardAllowanceAmount"
                        CHECK ("StandardAllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardWorkdayCount"
                        CHECK ("StandardWorkdayCount" > 0),
                    CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualWorkdayCount"
                        CHECK ("ActualWorkdayCount" >= 0),
                    CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualVsStandardWorkdayCount"
                        CHECK ("ActualWorkdayCount" <= "StandardWorkdayCount"),
                    CONSTRAINT "CK_payroll_decuction_summary_insurance_details_AttendanceRate"
                        CHECK ("AttendanceRate" >= 0 AND "AttendanceRate" <= 1),
                    CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualAllowanceAmount"
                        CHECK ("ActualAllowanceAmount" >= 0)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId"
                ON public.payroll_decuction_summary_insurance_details ("PayrollDeductionSummaryRecordId");

                CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_insurance_details_IsLocked"
                ON public.payroll_decuction_summary_insurance_details ("IsLocked");

                DO $$
                BEGIN
                    IF to_regclass('public.payroll_decuction_summary_records') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId'
                        )
                    THEN
                        ALTER TABLE public.payroll_decuction_summary_insurance_details
                        ADD CONSTRAINT "FK_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId"
                        FOREIGN KEY ("PayrollDeductionSummaryRecordId")
                        REFERENCES public.payroll_decuction_summary_records ("Id")
                        ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS public.payroll_decuction_summary_insurance_details;
                """);
        }
    }
}
