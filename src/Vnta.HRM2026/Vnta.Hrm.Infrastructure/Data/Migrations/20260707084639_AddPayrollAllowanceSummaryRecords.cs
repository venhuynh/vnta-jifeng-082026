using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollAllowanceSummaryRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.shift_scheduling_settings
                ADD COLUMN IF NOT EXISTS "EffectiveFromDate" date NULL;

                ALTER TABLE IF EXISTS public.shift_scheduling_settings
                ADD COLUMN IF NOT EXISTS "EffectiveToDate" date NULL;

                CREATE TABLE IF NOT EXISTS public.payroll_allowance_summary_records (
                    "Id" uuid NOT NULL,
                    "EmployeeId" uuid NOT NULL,
                    "PayrollMonth" smallint NOT NULL,
                    "PayrollYear" smallint NOT NULL,
                    "ResponsibilityAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "SeniorityAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "AttendanceAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "MealAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "HazardAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "OtherAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "LeaveHolidayAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "IsLocked" boolean NOT NULL DEFAULT FALSE,
                    "Note" text NULL,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "CreatedBy" character varying(128) NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone NULL,
                    "UpdatedBy" character varying(128) NULL,
                    CONSTRAINT "PK_payroll_allowance_summary_records" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_payroll_allowance_summary_records_PayrollMonth"
                        CHECK ("PayrollMonth" >= 1 AND "PayrollMonth" <= 12),
                    CONSTRAINT "CK_payroll_allowance_summary_records_PayrollYear"
                        CHECK ("PayrollYear" >= 1 AND "PayrollYear" <= 9999),
                    CONSTRAINT "CK_payroll_allowance_summary_records_ResponsibilityAllowanceAmount"
                        CHECK ("ResponsibilityAllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_records_SeniorityAllowanceAmount"
                        CHECK ("SeniorityAllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_records_AttendanceAllowanceAmount"
                        CHECK ("AttendanceAllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_records_MealAllowanceAmount"
                        CHECK ("MealAllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_records_HazardAllowanceAmount"
                        CHECK ("HazardAllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_records_OtherAllowanceAmount"
                        CHECK ("OtherAllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_records_LeaveHolidayAllowanceAmount"
                        CHECK ("LeaveHolidayAllowanceAmount" >= 0)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_allowance_summary_records_EmployeeId_PayrollYear_PayrollMonth"
                ON public.payroll_allowance_summary_records ("EmployeeId", "PayrollYear", "PayrollMonth");

                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_summary_records_PayrollYear_PayrollMonth"
                ON public.payroll_allowance_summary_records ("PayrollYear", "PayrollMonth");

                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_summary_records_IsLocked"
                ON public.payroll_allowance_summary_records ("IsLocked");

                DO $$
                BEGIN
                    IF to_regclass('public.employees') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_summary_records_employees_EmployeeId'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_summary_records
                        ADD CONSTRAINT "FK_payroll_allowance_summary_records_employees_EmployeeId"
                        FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id")
                        ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE TABLE IF NOT EXISTS public.payroll_employee_responsibility_allowances (
                    "Id" uuid NOT NULL,
                    "EmployeeId" uuid NOT NULL,
                    "Year" integer NOT NULL,
                    "Month" integer NOT NULL,
                    "EmployeeCode" character varying(20) NOT NULL,
                    "EmployeeName" character varying(128) NOT NULL,
                    "PositionName" character varying(128) NULL,
                    "AllowanceAmount" numeric(18,2) NOT NULL,
                    "EffectiveFrom" date NOT NULL,
                    "EffectiveTo" date NULL,
                    "IsActive" boolean NOT NULL DEFAULT TRUE,
                    "Notes" character varying(500) NULL,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone NULL,
                    CONSTRAINT "PK_payroll_employee_responsibility_allowances" PRIMARY KEY ("Id")
                );

                CREATE INDEX IF NOT EXISTS "IX_payroll_employee_responsibility_allowances_EmployeeId"
                ON public.payroll_employee_responsibility_allowances ("EmployeeId");

                CREATE INDEX IF NOT EXISTS "IX_payroll_employee_responsibility_allowances_Year_Month"
                ON public.payroll_employee_responsibility_allowances ("Year", "Month");

                CREATE INDEX IF NOT EXISTS "IX_payroll_employee_responsibility_allowances_Year_Month_EmployeeCode"
                ON public.payroll_employee_responsibility_allowances ("Year", "Month", "EmployeeCode");

                DO $$
                BEGIN
                    IF to_regclass('public.employees') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_employee_responsibility_allowances_employees_EmployeeId'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_responsibility_allowances
                        ADD CONSTRAINT "FK_payroll_employee_responsibility_allowances_employees_EmployeeId"
                        FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id")
                        ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS public.payroll_allowance_summary_records;
                DROP TABLE IF EXISTS public.payroll_employee_responsibility_allowances;

                ALTER TABLE IF EXISTS public.shift_scheduling_settings
                DROP COLUMN IF EXISTS "EffectiveFromDate";

                ALTER TABLE IF EXISTS public.shift_scheduling_settings
                DROP COLUMN IF EXISTS "EffectiveToDate";
                """);
        }
    }
}
