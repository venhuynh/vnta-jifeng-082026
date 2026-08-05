using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Repairs databases where the legacy insurance-detail table was renamed before
/// all columns required by <c>PayrollDeductionInsuranceRecordRow</c> existed.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260722093000_ReconcilePayrollDeductionInsuranceRecordSchema")]
public partial class ReconcilePayrollDeductionInsuranceRecordSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                had_standard_allowance boolean := EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'payroll_decuction_insurance_records'
                      AND column_name = 'StandardAllowanceAmount');
                had_standard_workdays boolean := EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'payroll_decuction_insurance_records'
                      AND column_name = 'StandardWorkdayCount');
                had_actual_workdays boolean := EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'payroll_decuction_insurance_records'
                      AND column_name = 'ActualWorkdayCount');
                had_attendance_rate boolean := EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'payroll_decuction_insurance_records'
                      AND column_name = 'AttendanceRate');
                had_actual_allowance boolean := EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'payroll_decuction_insurance_records'
                      AND column_name = 'ActualAllowanceAmount');
                had_lock_state boolean := EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'payroll_decuction_insurance_records'
                      AND column_name = 'IsLocked');
                had_created_at boolean := EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'payroll_decuction_insurance_records'
                      AND column_name = 'CreatedAtUtc');
            BEGIN
                ALTER TABLE public.payroll_decuction_insurance_records
                    ADD COLUMN IF NOT EXISTS "StandardAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "StandardWorkdayCount" numeric(10,2) NOT NULL DEFAULT 1,
                    ADD COLUMN IF NOT EXISTS "ActualWorkdayCount" numeric(10,2) NOT NULL DEFAULT 1,
                    ADD COLUMN IF NOT EXISTS "AttendanceRate" numeric(7,4) NOT NULL DEFAULT 1,
                    ADD COLUMN IF NOT EXISTS "ActualAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "IsLocked" boolean NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS "CreatedAtUtc" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ADD COLUMN IF NOT EXISTS "UpdatedAtUtc" timestamp without time zone NULL;

                IF NOT had_standard_allowance THEN
                    UPDATE public.payroll_decuction_insurance_records insurance
                    SET "StandardAllowanceAmount" = COALESCE(summary."SocialInsuranceDeductionAmount", 0)
                    FROM public.payroll_decuction_summary_records summary
                    WHERE summary."Id" = insurance."PayrollDeductionSummaryRecordId";
                END IF;

                IF NOT had_standard_workdays THEN
                    UPDATE public.payroll_decuction_insurance_records
                    SET "StandardWorkdayCount" = 1;
                END IF;

                IF NOT had_actual_workdays THEN
                    UPDATE public.payroll_decuction_insurance_records
                    SET "ActualWorkdayCount" = "StandardWorkdayCount";
                END IF;

                IF NOT had_attendance_rate THEN
                    UPDATE public.payroll_decuction_insurance_records
                    SET "AttendanceRate" = CASE
                        WHEN "StandardWorkdayCount" > 0
                            THEN LEAST(1, GREATEST(0, "ActualWorkdayCount" / "StandardWorkdayCount"))
                        ELSE 0
                    END;
                END IF;

                IF NOT had_actual_allowance THEN
                    UPDATE public.payroll_decuction_insurance_records insurance
                    SET "ActualAllowanceAmount" = COALESCE(summary."SocialInsuranceDeductionAmount", 0)
                    FROM public.payroll_decuction_summary_records summary
                    WHERE summary."Id" = insurance."PayrollDeductionSummaryRecordId";
                END IF;

                IF NOT had_lock_state THEN
                    UPDATE public.payroll_decuction_insurance_records insurance
                    SET "IsLocked" = summary."IsLocked"
                    FROM public.payroll_decuction_summary_records summary
                    WHERE summary."Id" = insurance."PayrollDeductionSummaryRecordId";
                END IF;

                IF NOT had_created_at THEN
                    UPDATE public.payroll_decuction_insurance_records insurance
                    SET "CreatedAtUtc" = summary."CreatedAtUtc"
                    FROM public.payroll_decuction_summary_records summary
                    WHERE summary."Id" = insurance."PayrollDeductionSummaryRecordId";
                END IF;
            END $$;

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_insurance_records_IsLocked"
                ON public.payroll_decuction_insurance_records ("IsLocked");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This is a data-preserving schema repair. Removing its columns would
        // recreate the production failure, so rollback intentionally does nothing.
    }
}
