using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

[Migration("20260710170000_AlignPayrollInsuranceDeductionSchema")]
public sealed class AlignPayrollInsuranceDeductionSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.payroll_decuction_summary_records (
                "Id" uuid NOT NULL,
                "EmployeeId" uuid NOT NULL,
                "PayrollMonth" smallint NOT NULL,
                "PayrollYear" smallint NOT NULL,
                "BhxhYtAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "CongDoanAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "ThueTncnAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "TamUngAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "KhacAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "Note" text NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "CreatedBy" character varying(128) NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                "UpdatedBy" character varying(128) NULL,
                CONSTRAINT "PK_payroll_decuction_summary_records" PRIMARY KEY ("Id")
            );

            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "BhxhYtAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "CongDoanAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "ThueTncnAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "TamUngAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "KhacAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "IsLocked" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "Note" text NULL;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "CreatedBy" character varying(128) NULL;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "UpdatedBy" character varying(128) NULL;

            UPDATE public.payroll_decuction_summary_records
            SET "CreatedBy" = 'system'
            WHERE "CreatedBy" IS NULL OR btrim("CreatedBy") = '';

            ALTER TABLE public.payroll_decuction_summary_records
                ALTER COLUMN "CreatedBy" SET NOT NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_decuction_summary_records_EmployeeId_PayrollYear_PayrollMonth"
            ON public.payroll_decuction_summary_records ("EmployeeId", "PayrollYear", "PayrollMonth");

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_records_PayrollYear_PayrollMonth"
            ON public.payroll_decuction_summary_records ("PayrollYear", "PayrollMonth");

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_records_IsLocked"
            ON public.payroll_decuction_summary_records ("IsLocked");

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_records_PayrollMonth'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    DROP CONSTRAINT "CK_payroll_decuction_summary_records_PayrollMonth";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_records
                ADD CONSTRAINT "CK_payroll_decuction_summary_records_PayrollMonth"
                CHECK ("PayrollMonth" >= 1 AND "PayrollMonth" <= 12);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_records_PayrollYear'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    DROP CONSTRAINT "CK_payroll_decuction_summary_records_PayrollYear";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_records
                ADD CONSTRAINT "CK_payroll_decuction_summary_records_PayrollYear"
                CHECK ("PayrollYear" >= 1 AND "PayrollYear" <= 9999);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_records_BhxhYtAmount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    DROP CONSTRAINT "CK_payroll_decuction_summary_records_BhxhYtAmount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_records
                ADD CONSTRAINT "CK_payroll_decuction_summary_records_BhxhYtAmount"
                CHECK ("BhxhYtAmount" >= 0);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_records_CongDoanAmount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    DROP CONSTRAINT "CK_payroll_decuction_summary_records_CongDoanAmount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_records
                ADD CONSTRAINT "CK_payroll_decuction_summary_records_CongDoanAmount"
                CHECK ("CongDoanAmount" >= 0);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_records_ThueTncnAmount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    DROP CONSTRAINT "CK_payroll_decuction_summary_records_ThueTncnAmount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_records
                ADD CONSTRAINT "CK_payroll_decuction_summary_records_ThueTncnAmount"
                CHECK ("ThueTncnAmount" >= 0);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_records_TamUngAmount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    DROP CONSTRAINT "CK_payroll_decuction_summary_records_TamUngAmount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_records
                ADD CONSTRAINT "CK_payroll_decuction_summary_records_TamUngAmount"
                CHECK ("TamUngAmount" >= 0);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_records_KhacAmount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    DROP CONSTRAINT "CK_payroll_decuction_summary_records_KhacAmount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_records
                ADD CONSTRAINT "CK_payroll_decuction_summary_records_KhacAmount"
                CHECK ("KhacAmount" >= 0);

                IF to_regclass('public.employees') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_payroll_decuction_summary_records_employees_EmployeeId'
                    )
                THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    ADD CONSTRAINT "FK_payroll_decuction_summary_records_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id")
                    ON DELETE RESTRICT;
                END IF;
            END $$;

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
                CONSTRAINT "PK_payroll_decuction_summary_insurance_details" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId"
            ON public.payroll_decuction_summary_insurance_details ("PayrollDeductionSummaryRecordId");

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_insurance_details_IsLocked"
            ON public.payroll_decuction_summary_insurance_details ("IsLocked");

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_insurance_details_StandardAllowanceAmount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    DROP CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardAllowanceAmount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_insurance_details
                ADD CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardAllowanceAmount"
                CHECK ("StandardAllowanceAmount" >= 0);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_insurance_details_StandardWorkdayCount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    DROP CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardWorkdayCount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_insurance_details
                ADD CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardWorkdayCount"
                CHECK ("StandardWorkdayCount" > 0);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_insurance_details_ActualWorkdayCount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    DROP CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualWorkdayCount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_insurance_details
                ADD CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualWorkdayCount"
                CHECK ("ActualWorkdayCount" >= 0);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_insurance_details_ActualVsStandardWorkdayCount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    DROP CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualVsStandardWorkdayCount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_insurance_details
                ADD CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualVsStandardWorkdayCount"
                CHECK ("ActualWorkdayCount" <= "StandardWorkdayCount");

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_insurance_details_AttendanceRate'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    DROP CONSTRAINT "CK_payroll_decuction_summary_insurance_details_AttendanceRate";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_insurance_details
                ADD CONSTRAINT "CK_payroll_decuction_summary_insurance_details_AttendanceRate"
                CHECK ("AttendanceRate" >= 0 AND "AttendanceRate" <= 1);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_insurance_details_ActualAllowanceAmount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    DROP CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualAllowanceAmount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_insurance_details
                ADD CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualAllowanceAmount"
                CHECK ("ActualAllowanceAmount" >= 0);

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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(string.Empty);
    }
}
