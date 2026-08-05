using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollEmployeeSeniorityAllowances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.payroll_employee_seniority_allowances (
                    "Id" uuid NOT NULL,
                    "PayrollAllowanceSummaryRecordId" uuid NOT NULL,
                    "EmploymentStartDate" date NULL,
                    "CompletedSeniorityYears" smallint NULL,
                    "CompletedSeniorityMonths" smallint NULL,
                    "SalaryWorkDays" numeric(9,2) NULL,
                    "AppliedRuleKey" character varying(32) NULL,
                    "AllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "Note" text NULL,
                    "IsLocked" boolean NOT NULL DEFAULT FALSE,
                    "RefreshedAtUtc" timestamp without time zone NULL,
                    "RefreshedBy" character varying(128) NULL,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "CreatedBy" character varying(128) NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone NULL,
                    "UpdatedBy" character varying(128) NULL,
                    CONSTRAINT "PK_payroll_employee_seniority_allowances" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_payroll_employee_seniority_allowances_CompletedSeniorityYears"
                        CHECK ("CompletedSeniorityYears" IS NULL OR "CompletedSeniorityYears" >= 0),
                    CONSTRAINT "CK_payroll_employee_seniority_allowances_CompletedSeniorityMonths"
                        CHECK ("CompletedSeniorityMonths" IS NULL OR ("CompletedSeniorityMonths" >= 0 AND "CompletedSeniorityMonths" < 12)),
                    CONSTRAINT "CK_payroll_employee_seniority_allowances_SalaryWorkDays"
                        CHECK ("SalaryWorkDays" IS NULL OR "SalaryWorkDays" >= 0),
                    CONSTRAINT "CK_payroll_employee_seniority_allowances_AllowanceAmount"
                        CHECK ("AllowanceAmount" >= 0)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId"
                    ON public.payroll_employee_seniority_allowances ("PayrollAllowanceSummaryRecordId");

                CREATE INDEX IF NOT EXISTS "IX_payroll_employee_seniority_allowances_IsLocked"
                    ON public.payroll_employee_seniority_allowances ("IsLocked");

                CREATE INDEX IF NOT EXISTS "IX_payroll_employee_seniority_allowances_AppliedRuleKey"
                    ON public.payroll_employee_seniority_allowances ("AppliedRuleKey");

                DO $$
                BEGIN
                    IF to_regclass('public.payroll_allowance_summary_records') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_seniority_allowances
                        ADD CONSTRAINT "FK_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId"
                        FOREIGN KEY ("PayrollAllowanceSummaryRecordId")
                        REFERENCES public.payroll_allowance_summary_records ("Id")
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
                DROP TABLE IF EXISTS public.payroll_employee_seniority_allowances;
                """);
        }
    }
}
