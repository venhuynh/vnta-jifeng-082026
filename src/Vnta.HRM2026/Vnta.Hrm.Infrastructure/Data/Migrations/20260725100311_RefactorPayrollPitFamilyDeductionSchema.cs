using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPayrollPitFamilyDeductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public.payroll_monthly_employee_pit_family_deductions') IS NULL
                       AND to_regclass('public.payroll_decuction_family_deduction_records') IS NOT NULL THEN
                        ALTER TABLE public.payroll_decuction_family_deduction_records RENAME TO payroll_monthly_employee_pit_family_deductions;
                    END IF;
                    IF to_regclass('public.payroll_monthly_employee_pit_family_deductions') IS NULL THEN
                        CREATE TABLE public.payroll_monthly_employee_pit_family_deductions ("Id" uuid NOT NULL PRIMARY KEY);
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'payroll_monthly_employee_pit_family_deductions' AND column_name = 'DependentCount')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'payroll_monthly_employee_pit_family_deductions' AND column_name = 'DependentDeductionCount') THEN
                        ALTER TABLE public.payroll_monthly_employee_pit_family_deductions RENAME COLUMN "DependentCount" TO "DependentDeductionCount";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'payroll_monthly_employee_pit_family_deductions' AND column_name = 'Note')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'payroll_monthly_employee_pit_family_deductions' AND column_name = 'GhiChu') THEN
                        ALTER TABLE public.payroll_monthly_employee_pit_family_deductions RENAME COLUMN "Note" TO "GhiChu";
                    END IF;
                END $$;

                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions
                    ADD COLUMN IF NOT EXISTS "PayrollDeductionSummaryRecordId" uuid NULL,
                    ADD COLUMN IF NOT EXISTS "IsLocked" boolean NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS "CreatedAtUtc" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ADD COLUMN IF NOT EXISTS "CreatedBy" character varying(128) NOT NULL DEFAULT 'system',
                    ADD COLUMN IF NOT EXISTS "UpdatedAtUtc" timestamp without time zone NULL,
                    ADD COLUMN IF NOT EXISTS "UpdatedBy" character varying(128) NULL,
                    ADD COLUMN IF NOT EXISTS "EmployeeId" uuid NULL,
                    ADD COLUMN IF NOT EXISTS "Nam" smallint NULL,
                    ADD COLUMN IF NOT EXISTS "Thang" smallint NULL,
                    ADD COLUMN IF NOT EXISTS "TaxResidenceStatus" smallint NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "IsSelfDeductionApplied" boolean NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS "SelfDeductionAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "DependentDeductionCount" smallint NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "DependentDeductionAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "TotalDependentDeductionAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "TotalFamilyDeductionAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "TaxPolicyCode" character varying(100) NOT NULL DEFAULT 'legacy-unresolved',
                    ADD COLUMN IF NOT EXISTS "PolicyEffectiveFrom" date NOT NULL DEFAULT DATE '1900-01-01',
                    ADD COLUMN IF NOT EXISTS "PolicyEffectiveTo" date NULL,
                    ADD COLUMN IF NOT EXISTS "IsManualOverride" boolean NOT NULL DEFAULT TRUE,
                    ADD COLUMN IF NOT EXISTS "LockedAtUtc" timestamp without time zone NULL,
                    ADD COLUMN IF NOT EXISTS "LockedBy" character varying(128) NULL,
                    ADD COLUMN IF NOT EXISTS "DependentSnapshotJson" jsonb NULL,
                    ADD COLUMN IF NOT EXISTS "CalculationSnapshotJson" jsonb NULL,
                    ADD COLUMN IF NOT EXISTS "GhiChu" text NULL;

                UPDATE public.payroll_monthly_employee_pit_family_deductions snapshot
                SET "EmployeeId" = summary."EmployeeId", "Nam" = summary."PayrollYear", "Thang" = summary."PayrollMonth",
                    "IsLocked" = snapshot."IsLocked" OR summary."IsLocked",
                    "LockedAtUtc" = CASE WHEN snapshot."IsLocked" OR summary."IsLocked" THEN COALESCE(snapshot."UpdatedAtUtc", snapshot."CreatedAtUtc") ELSE NULL END,
                    "DependentSnapshotJson" = COALESCE(snapshot."DependentSnapshotJson", jsonb_build_object('source', 'legacy-payroll_decuction_family_deduction_records', 'dependentCount', snapshot."DependentDeductionCount")),
                    "CalculationSnapshotJson" = COALESCE(snapshot."CalculationSnapshotJson", jsonb_build_object('backfilled', true, 'reason', 'Legacy table did not store deduction amounts or policy'))
                FROM public.payroll_decuction_summary_records summary
                WHERE snapshot."PayrollDeductionSummaryRecordId" = summary."Id";

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM public.payroll_monthly_employee_pit_family_deductions WHERE "EmployeeId" IS NULL OR "Nam" IS NULL OR "Thang" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot safely backfill family-deduction snapshots without a payroll summary.';
                    END IF;
                END $$;

                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions
                    ALTER COLUMN "PayrollDeductionSummaryRecordId" DROP NOT NULL,
                    ALTER COLUMN "EmployeeId" SET NOT NULL,
                    ALTER COLUMN "Nam" SET NOT NULL,
                    ALTER COLUMN "Thang" SET NOT NULL;

                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions
                    DROP CONSTRAINT IF EXISTS "FK_payroll_decuction_family_deduction_records_payroll_decuctio~",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_family_deduction_records_DependentCount",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_Thang",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_Nam",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_Amounts",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_SelfApplied",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_DependentTotal",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_FamilyTotal",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_PolicyRange";
                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions
                    ADD CONSTRAINT "CK_payroll_monthly_employee_pit_family_deductions_Thang" CHECK ("Thang" BETWEEN 1 AND 12),
                    ADD CONSTRAINT "CK_payroll_monthly_employee_pit_family_deductions_Nam" CHECK ("Nam" BETWEEN 1900 AND 2100),
                    ADD CONSTRAINT "CK_payroll_monthly_employee_pit_family_deductions_Amounts" CHECK ("SelfDeductionAmount" >= 0 AND "DependentDeductionCount" >= 0 AND "DependentDeductionAmount" >= 0 AND "TotalDependentDeductionAmount" >= 0 AND "TotalFamilyDeductionAmount" >= 0),
                    ADD CONSTRAINT "CK_payroll_monthly_employee_pit_family_deductions_SelfApplied" CHECK ("IsSelfDeductionApplied" OR "SelfDeductionAmount" = 0),
                    ADD CONSTRAINT "CK_payroll_monthly_employee_pit_family_deductions_DependentTotal" CHECK ("TotalDependentDeductionAmount" = "DependentDeductionCount" * "DependentDeductionAmount"),
                    ADD CONSTRAINT "CK_payroll_monthly_employee_pit_family_deductions_FamilyTotal" CHECK ("TotalFamilyDeductionAmount" = "SelfDeductionAmount" + "TotalDependentDeductionAmount"),
                    ADD CONSTRAINT "CK_payroll_monthly_employee_pit_family_deductions_PolicyRange" CHECK ("PolicyEffectiveTo" IS NULL OR "PolicyEffectiveTo" >= "PolicyEffectiveFrom");
                CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_monthly_employee_pit_family_deductions_EmployeeId_Nam_Thang" ON public.payroll_monthly_employee_pit_family_deductions ("EmployeeId", "Nam", "Thang");
                CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_monthly_employee_pit_family_deductions_SummaryId" ON public.payroll_monthly_employee_pit_family_deductions ("PayrollDeductionSummaryRecordId");
                CREATE INDEX IF NOT EXISTS "IX_payroll_monthly_employee_pit_family_deductions_Nam_Thang" ON public.payroll_monthly_employee_pit_family_deductions ("Nam", "Thang");
                CREATE INDEX IF NOT EXISTS "IX_payroll_monthly_employee_pit_family_deductions_IsLocked" ON public.payroll_monthly_employee_pit_family_deductions ("IsLocked");
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_payroll_monthly_employee_pit_family_deductions_employees_EmployeeId') THEN
                        ALTER TABLE public.payroll_monthly_employee_pit_family_deductions ADD CONSTRAINT "FK_payroll_monthly_employee_pit_family_deductions_employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_payroll_monthly_employee_pit_family_deductions_summary_SummaryId') THEN
                        ALTER TABLE public.payroll_monthly_employee_pit_family_deductions ADD CONSTRAINT "FK_payroll_monthly_employee_pit_family_deductions_summary_SummaryId" FOREIGN KEY ("PayrollDeductionSummaryRecordId") REFERENCES public.payroll_decuction_summary_records ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE TABLE IF NOT EXISTS public.payroll_employee_tax_dependents (
                    "Id" uuid NOT NULL PRIMARY KEY, "EmployeeId" uuid NOT NULL, "FullName" character varying(256) NOT NULL,
                    "Gender" character varying(32), "DateOfBirth" date, "IdentityDocumentNumber" character varying(64), "TaxCode" character varying(64),
                    "Nationality" character varying(128), "Relationship" character varying(128), "IsRegisteredForFamilyDeduction" boolean NOT NULL DEFAULT FALSE,
                    "RegistrationBookNumber" character varying(128), "RegistrationNumber" character varying(128), "AdministrativeAddress" text,
                    "DeductionFromMonth" date, "DeductionToMonth" date, "Note" text, "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "CreatedBy" character varying(128) NOT NULL, "UpdatedAtUtc" timestamp without time zone, "UpdatedBy" character varying(128),
                    CONSTRAINT "CK_payroll_employee_tax_dependents_DeductionRange" CHECK ("DeductionToMonth" IS NULL OR "DeductionFromMonth" IS NULL OR "DeductionToMonth" >= "DeductionFromMonth"),
                    CONSTRAINT "FK_payroll_employee_tax_dependents_employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT);
                CREATE INDEX IF NOT EXISTS "IX_payroll_employee_tax_dependents_EmployeeId" ON public.payroll_employee_tax_dependents ("EmployeeId");
                CREATE INDEX IF NOT EXISTS "IX_payroll_employee_tax_dependents_EmployeeId_Registered" ON public.payroll_employee_tax_dependents ("EmployeeId", "IsRegisteredForFamilyDeduction");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS public.payroll_employee_tax_dependents;
                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions
                    DROP CONSTRAINT IF EXISTS "FK_payroll_monthly_employee_pit_family_deductions_employees_EmployeeId",
                    DROP CONSTRAINT IF EXISTS "FK_payroll_monthly_employee_pit_family_deductions_summary_SummaryId",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_Thang",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_Nam",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_Amounts",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_SelfApplied",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_DependentTotal",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_FamilyTotal",
                    DROP CONSTRAINT IF EXISTS "CK_payroll_monthly_employee_pit_family_deductions_PolicyRange";
                DROP INDEX IF EXISTS public."UX_payroll_monthly_employee_pit_family_deductions_EmployeeId_Nam_Thang";
                DROP INDEX IF EXISTS public."UX_payroll_monthly_employee_pit_family_deductions_SummaryId";
                DROP INDEX IF EXISTS public."IX_payroll_monthly_employee_pit_family_deductions_Nam_Thang";
                DROP INDEX IF EXISTS public."IX_payroll_monthly_employee_pit_family_deductions_IsLocked";
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM public.payroll_monthly_employee_pit_family_deductions WHERE "PayrollDeductionSummaryRecordId" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot roll back family-deduction schema while snapshots without a legacy payroll summary exist.';
                    END IF;
                END $$;
                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions RENAME COLUMN "DependentDeductionCount" TO "DependentCount";
                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions RENAME COLUMN "GhiChu" TO "Note";
                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions
                    DROP COLUMN "EmployeeId", DROP COLUMN "Nam", DROP COLUMN "Thang", DROP COLUMN "TaxResidenceStatus",
                    DROP COLUMN "IsSelfDeductionApplied", DROP COLUMN "SelfDeductionAmount", DROP COLUMN "DependentDeductionAmount",
                    DROP COLUMN "TotalDependentDeductionAmount", DROP COLUMN "TotalFamilyDeductionAmount", DROP COLUMN "TaxPolicyCode",
                    DROP COLUMN "PolicyEffectiveFrom", DROP COLUMN "PolicyEffectiveTo", DROP COLUMN "IsManualOverride", DROP COLUMN "LockedAtUtc",
                    DROP COLUMN "LockedBy", DROP COLUMN "DependentSnapshotJson", DROP COLUMN "CalculationSnapshotJson";
                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions ALTER COLUMN "PayrollDeductionSummaryRecordId" SET NOT NULL;
                ALTER TABLE public.payroll_monthly_employee_pit_family_deductions RENAME TO payroll_decuction_family_deduction_records;
                ALTER TABLE public.payroll_decuction_family_deduction_records
                    ADD CONSTRAINT "CK_payroll_decuction_family_deduction_records_DependentCount" CHECK ("DependentCount" >= 0),
                    ADD CONSTRAINT "FK_payroll_decuction_family_deduction_records_payroll_decuctio~" FOREIGN KEY ("PayrollDeductionSummaryRecordId") REFERENCES public.payroll_decuction_summary_records ("Id") ON DELETE CASCADE;
                CREATE UNIQUE INDEX "UX_payroll_decuction_family_deduction_records_SummaryId" ON public.payroll_decuction_family_deduction_records ("PayrollDeductionSummaryRecordId");
                CREATE INDEX "IX_payroll_decuction_family_deduction_records_IsLocked" ON public.payroll_decuction_family_deduction_records ("IsLocked");
                """);
        }
    }
}
