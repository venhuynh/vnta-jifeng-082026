using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class RestoreMealAllowanceSummaryReference : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // A previously applied raw-SQL migration removed this key from databases that
        // already use the normalized table. Restore it without assuming the legacy
        // table still exists, then reconnect each detail row to its period summary.
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                primary_key_name text;
            BEGIN
                IF to_regclass('public.payroll_allowance_meal_records') IS NULL THEN
                    RETURN;
                END IF;

                ALTER TABLE public.payroll_allowance_meal_records
                ADD COLUMN IF NOT EXISTS "PayrollAllowanceSummaryRecordId" uuid;

                UPDATE public.payroll_allowance_summary_records AS summary
                SET
                    "MealAllowanceAmount" = meal."MealAllowanceAmount",
                    "UpdatedAtUtc" = COALESCE(meal."UpdatedAtUtc", summary."UpdatedAtUtc"),
                    "UpdatedBy" = COALESCE(meal."UpdatedBy", summary."UpdatedBy")
                FROM public.payroll_allowance_meal_records AS meal
                WHERE summary."EmployeeId" = meal."EmployeeId"
                  AND summary."PayrollYear" = meal."PayrollYear"
                  AND summary."PayrollMonth" = meal."PayrollMonth";

                UPDATE public.payroll_allowance_meal_records AS meal
                SET "PayrollAllowanceSummaryRecordId" = summary."Id"
                FROM public.payroll_allowance_summary_records AS summary
                WHERE meal."PayrollAllowanceSummaryRecordId" IS NULL
                  AND summary."EmployeeId" = meal."EmployeeId"
                  AND summary."PayrollYear" = meal."PayrollYear"
                  AND summary."PayrollMonth" = meal."PayrollMonth";

                IF EXISTS (
                    SELECT 1
                    FROM public.payroll_allowance_meal_records
                    WHERE "PayrollAllowanceSummaryRecordId" IS NULL) THEN
                    RAISE EXCEPTION
                        'Cannot restore payroll_allowance_meal_records: a meal record has no matching payroll allowance summary.';
                END IF;

                SELECT constraint_name
                INTO primary_key_name
                FROM information_schema.table_constraints
                WHERE table_schema = 'public'
                  AND table_name = 'payroll_allowance_meal_records'
                  AND constraint_type = 'PRIMARY KEY';

                IF primary_key_name IS NOT NULL THEN
                    EXECUTE format(
                        'ALTER TABLE public.payroll_allowance_meal_records DROP CONSTRAINT %I',
                        primary_key_name);
                END IF;

                ALTER TABLE public.payroll_allowance_meal_records
                ALTER COLUMN "PayrollAllowanceSummaryRecordId" SET NOT NULL;

                ALTER TABLE public.payroll_allowance_meal_records
                ADD CONSTRAINT "PK_payroll_allowance_meal_records"
                PRIMARY KEY ("PayrollAllowanceSummaryRecordId");

                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conrelid = 'public.payroll_allowance_meal_records'::regclass
                      AND confrelid = 'public.payroll_allowance_summary_records'::regclass
                      AND contype = 'f') THEN
                    ALTER TABLE public.payroll_allowance_meal_records
                    ADD CONSTRAINT "FK_payroll_allowance_meal_records_payroll_allowance_summary_records_PayrollAllowanceSummaryRecordId"
                    FOREIGN KEY ("PayrollAllowanceSummaryRecordId")
                    REFERENCES public.payroll_allowance_summary_records ("Id")
                    ON DELETE CASCADE;
                END IF;

                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_meal_records_IsLocked"
                    ON public.payroll_allowance_meal_records ("IsLocked");
                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_meal_records_PayrollYear_PayrollMonth"
                    ON public.payroll_allowance_meal_records ("PayrollYear", "PayrollMonth");
                CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_allowance_meal_records_EmployeeId_PayrollYear_PayrollMonth"
                    ON public.payroll_allowance_meal_records ("EmployeeId", "PayrollYear", "PayrollMonth");
            END $$;
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.employee_citizen_identities (
                "EmployeeId" uuid NOT NULL,
                "CitizenIdentityNumberCiphertext" text NOT NULL,
                "CitizenIdentityNumberHash" char(64) NOT NULL,
                "IssuedDate" date NULL,
                "IssuedPlace" character varying(250) NULL,
                "ExpiryDate" date NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_employee_citizen_identities" PRIMARY KEY ("EmployeeId"),
                CONSTRAINT "FK_employee_citizen_identities_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_employee_citizen_identities_NumberHash"
                ON public.employee_citizen_identities ("CitizenIdentityNumberHash");

            CREATE TABLE IF NOT EXISTS public.employee_contact_profiles (
                "EmployeeId" uuid NOT NULL,
                "PersonalEmail" character varying(256) NULL,
                "PersonalPhoneNumber" character varying(30) NULL,
                "PermanentAddress" text NULL,
                "CurrentAddress" text NULL,
                "EmergencyContactName" character varying(150) NULL,
                "EmergencyContactRelationship" character varying(100) NULL,
                "EmergencyContactPhoneNumber" character varying(30) NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_employee_contact_profiles" PRIMARY KEY ("EmployeeId"),
                CONSTRAINT "FK_employee_contact_profiles_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The repaired schema can predate this migration on affected databases, so a
        // rollback must not remove shared tables or detail data it did not create.
    }
}
