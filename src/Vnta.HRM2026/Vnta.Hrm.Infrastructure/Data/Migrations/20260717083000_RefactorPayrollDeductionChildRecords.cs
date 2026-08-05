using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260717083000_RefactorPayrollDeductionChildRecords")]
public partial class RefactorPayrollDeductionChildRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF to_regclass('public.payroll_decuction_summary_insurance_details') IS NOT NULL
                   AND to_regclass('public.payroll_decuction_insurance_records') IS NULL THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    RENAME TO payroll_decuction_insurance_records;
                END IF;
            END $$;

            CREATE TABLE IF NOT EXISTS public.payroll_decuction_insurance_records (
                "PayrollDeductionSummaryRecordId" uuid NOT NULL,
                "StandardAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "StandardWorkdayCount" numeric(10,2) NOT NULL DEFAULT 1,
                "ActualWorkdayCount" numeric(10,2) NOT NULL DEFAULT 0,
                "AttendanceRate" numeric(7,4) NOT NULL DEFAULT 0,
                "ActualAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            );

            ALTER TABLE public.payroll_decuction_insurance_records
                DROP CONSTRAINT IF EXISTS "PK_payroll_decuction_summary_insurance_details";
            ALTER TABLE public.payroll_decuction_insurance_records
                DROP CONSTRAINT IF EXISTS "PK_payroll_decuction_insurance_records";
            ALTER TABLE public.payroll_decuction_insurance_records
                DROP COLUMN IF EXISTS "Id";
            ALTER TABLE public.payroll_decuction_insurance_records
                ADD CONSTRAINT "PK_payroll_decuction_insurance_records"
                PRIMARY KEY ("PayrollDeductionSummaryRecordId");
            ALTER TABLE public.payroll_decuction_insurance_records
                DROP CONSTRAINT IF EXISTS "FK_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId";
            ALTER TABLE public.payroll_decuction_insurance_records
                DROP CONSTRAINT IF EXISTS "FK_payroll_decuction_insurance_records_PayrollDeductionSummaryRecordId";
            ALTER TABLE public.payroll_decuction_insurance_records
                ADD CONSTRAINT "FK_payroll_decuction_insurance_records_PayrollDeductionSummaryRecordId"
                FOREIGN KEY ("PayrollDeductionSummaryRecordId")
                REFERENCES public.payroll_decuction_summary_records ("Id") ON DELETE CASCADE;
            DROP INDEX IF EXISTS public."UX_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId";
            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_insurance_records_IsLocked"
                ON public.payroll_decuction_insurance_records ("IsLocked");

            CREATE TABLE IF NOT EXISTS public.payroll_decuction_tax_records (
                "PayrollDeductionSummaryRecordId" uuid NOT NULL,
                "DeductionAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_payroll_decuction_tax_records" PRIMARY KEY ("PayrollDeductionSummaryRecordId"),
                CONSTRAINT "FK_payroll_decuction_tax_records_PayrollDeductionSummaryRecordId"
                    FOREIGN KEY ("PayrollDeductionSummaryRecordId")
                    REFERENCES public.payroll_decuction_summary_records ("Id") ON DELETE CASCADE,
                CONSTRAINT "CK_payroll_decuction_tax_records_DeductionAmount" CHECK ("DeductionAmount" >= 0)
            );
            CREATE TABLE IF NOT EXISTS public.payroll_decuction_union_fee_records (
                "PayrollDeductionSummaryRecordId" uuid NOT NULL,
                "DeductionAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_payroll_decuction_union_fee_records" PRIMARY KEY ("PayrollDeductionSummaryRecordId"),
                CONSTRAINT "FK_payroll_decuction_union_fee_records_PayrollDeductionSummaryRecordId"
                    FOREIGN KEY ("PayrollDeductionSummaryRecordId")
                    REFERENCES public.payroll_decuction_summary_records ("Id") ON DELETE CASCADE,
                CONSTRAINT "CK_payroll_decuction_union_fee_records_DeductionAmount" CHECK ("DeductionAmount" >= 0)
            );
            CREATE TABLE IF NOT EXISTS public.payroll_decuction_advance_records (
                "PayrollDeductionSummaryRecordId" uuid NOT NULL,
                "DeductionAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_payroll_decuction_advance_records" PRIMARY KEY ("PayrollDeductionSummaryRecordId"),
                CONSTRAINT "FK_payroll_decuction_advance_records_PayrollDeductionSummaryRecordId"
                    FOREIGN KEY ("PayrollDeductionSummaryRecordId")
                    REFERENCES public.payroll_decuction_summary_records ("Id") ON DELETE CASCADE,
                CONSTRAINT "CK_payroll_decuction_advance_records_DeductionAmount" CHECK ("DeductionAmount" >= 0)
            );
            CREATE TABLE IF NOT EXISTS public.payroll_decuction_other_records (
                "PayrollDeductionSummaryRecordId" uuid NOT NULL,
                "DeductionAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_payroll_decuction_other_records" PRIMARY KEY ("PayrollDeductionSummaryRecordId"),
                CONSTRAINT "FK_payroll_decuction_other_records_PayrollDeductionSummaryRecordId"
                    FOREIGN KEY ("PayrollDeductionSummaryRecordId")
                    REFERENCES public.payroll_decuction_summary_records ("Id") ON DELETE CASCADE,
                CONSTRAINT "CK_payroll_decuction_other_records_DeductionAmount" CHECK ("DeductionAmount" >= 0)
            );

            INSERT INTO public.payroll_decuction_insurance_records (
                "PayrollDeductionSummaryRecordId", "StandardAllowanceAmount", "StandardWorkdayCount", "ActualWorkdayCount", "AttendanceRate", "ActualAllowanceAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc")
            SELECT "Id", "BhxhYtAmount", 1, 1, 1, "BhxhYtAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc"
            FROM public.payroll_decuction_summary_records
            WHERE NOT EXISTS (
                SELECT 1 FROM public.payroll_decuction_insurance_records insurance
                WHERE insurance."PayrollDeductionSummaryRecordId" = payroll_decuction_summary_records."Id");
            INSERT INTO public.payroll_decuction_tax_records ("PayrollDeductionSummaryRecordId", "DeductionAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc")
            SELECT "Id", "ThueTncnAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc" FROM public.payroll_decuction_summary_records
            ON CONFLICT ("PayrollDeductionSummaryRecordId") DO NOTHING;
            INSERT INTO public.payroll_decuction_union_fee_records ("PayrollDeductionSummaryRecordId", "DeductionAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc")
            SELECT "Id", "CongDoanAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc" FROM public.payroll_decuction_summary_records
            ON CONFLICT ("PayrollDeductionSummaryRecordId") DO NOTHING;
            INSERT INTO public.payroll_decuction_advance_records ("PayrollDeductionSummaryRecordId", "DeductionAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc")
            SELECT "Id", "TamUngAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc" FROM public.payroll_decuction_summary_records
            ON CONFLICT ("PayrollDeductionSummaryRecordId") DO NOTHING;
            INSERT INTO public.payroll_decuction_other_records ("PayrollDeductionSummaryRecordId", "DeductionAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc")
            SELECT "Id", "KhacAmount", "IsLocked", "CreatedAtUtc", "UpdatedAtUtc" FROM public.payroll_decuction_summary_records
            ON CONFLICT ("PayrollDeductionSummaryRecordId") DO NOTHING;

            ALTER TABLE public.payroll_decuction_summary_records DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_summary_records_BhxhYtAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_summary_records_CongDoanAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_summary_records_ThueTncnAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_summary_records_TamUngAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_summary_records_KhacAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP COLUMN IF EXISTS "BhxhYtAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP COLUMN IF EXISTS "CongDoanAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP COLUMN IF EXISTS "ThueTncnAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP COLUMN IF EXISTS "TamUngAmount";
            ALTER TABLE public.payroll_decuction_summary_records DROP COLUMN IF EXISTS "KhacAmount";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.payroll_decuction_summary_records ADD COLUMN IF NOT EXISTS "BhxhYtAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records ADD COLUMN IF NOT EXISTS "CongDoanAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records ADD COLUMN IF NOT EXISTS "ThueTncnAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records ADD COLUMN IF NOT EXISTS "TamUngAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records ADD COLUMN IF NOT EXISTS "KhacAmount" numeric(18,2) NOT NULL DEFAULT 0;

            UPDATE public.payroll_decuction_summary_records summary
            SET "BhxhYtAmount" = COALESCE(insurance."ActualAllowanceAmount", 0),
                "CongDoanAmount" = COALESCE(unionFee."DeductionAmount", 0),
                "ThueTncnAmount" = COALESCE(tax."DeductionAmount", 0),
                "TamUngAmount" = COALESCE(advance."DeductionAmount", 0),
                "KhacAmount" = COALESCE(otherRecord."DeductionAmount", 0)
            FROM public.payroll_decuction_insurance_records insurance
            LEFT JOIN public.payroll_decuction_tax_records tax ON tax."PayrollDeductionSummaryRecordId" = summary."Id"
            LEFT JOIN public.payroll_decuction_union_fee_records unionFee ON unionFee."PayrollDeductionSummaryRecordId" = summary."Id"
            LEFT JOIN public.payroll_decuction_advance_records advance ON advance."PayrollDeductionSummaryRecordId" = summary."Id"
            LEFT JOIN public.payroll_decuction_other_records otherRecord ON otherRecord."PayrollDeductionSummaryRecordId" = summary."Id"
            WHERE insurance."PayrollDeductionSummaryRecordId" = summary."Id";

            DROP TABLE IF EXISTS public.payroll_decuction_other_records;
            DROP TABLE IF EXISTS public.payroll_decuction_advance_records;
            DROP TABLE IF EXISTS public.payroll_decuction_union_fee_records;
            DROP TABLE IF EXISTS public.payroll_decuction_tax_records;
            DROP TABLE IF EXISTS public.payroll_decuction_insurance_records;
            """);
    }
}
