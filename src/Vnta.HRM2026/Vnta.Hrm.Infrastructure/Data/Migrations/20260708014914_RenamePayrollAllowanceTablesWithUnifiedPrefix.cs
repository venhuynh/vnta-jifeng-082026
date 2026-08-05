using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamePayrollAllowanceTablesWithUnifiedPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public.payroll_allowance_responsibility_records') IS NULL
                        AND to_regclass('public.payroll_employee_responsibility_allowances') IS NOT NULL
                    THEN
                        ALTER TABLE public.payroll_employee_responsibility_allowances
                        RENAME TO payroll_allowance_responsibility_records;
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'PK_payroll_employee_responsibility_allowances'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'PK_payroll_allowance_responsibility_records'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_responsibility_records
                        RENAME CONSTRAINT "PK_payroll_employee_responsibility_allowances"
                        TO "PK_payroll_allowance_responsibility_records";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_employee_responsibility_allowances_employees_EmployeeId'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_responsibility_records_employees_EmployeeId'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_responsibility_records
                        RENAME CONSTRAINT "FK_payroll_employee_responsibility_allowances_employees_EmployeeId"
                        TO "FK_payroll_allowance_responsibility_records_employees_EmployeeId";
                    END IF;
                END $$;

                ALTER INDEX IF EXISTS "IX_payroll_employee_responsibility_allowances_Year_Month"
                RENAME TO "IX_payroll_allowance_responsibility_records_Year_Month";

                ALTER INDEX IF EXISTS "IX_payroll_employee_responsibility_allowances_Year_Month_EmployeeCode"
                RENAME TO "IX_payroll_allowance_responsibility_records_Year_Month_EmployeeCode";

                ALTER INDEX IF EXISTS "IX_payroll_employee_responsibility_allowances_EmployeeId"
                RENAME TO "IX_payroll_allowance_responsibility_records_EmployeeId";

                DO $$
                BEGIN
                    IF to_regclass('public.payroll_allowance_seniority_records') IS NULL
                        AND to_regclass('public.payroll_employee_seniority_allowances') IS NOT NULL
                    THEN
                        ALTER TABLE public.payroll_employee_seniority_allowances
                        RENAME TO payroll_allowance_seniority_records;
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'PK_payroll_employee_seniority_allowances'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'PK_payroll_allowance_seniority_records'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        RENAME CONSTRAINT "PK_payroll_employee_seniority_allowances"
                        TO "PK_payroll_allowance_seniority_records";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_employee_seniority_allowances_CompletedSeniorityYears'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_allowance_seniority_records_CompletedSeniorityYears'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        RENAME CONSTRAINT "CK_payroll_employee_seniority_allowances_CompletedSeniorityYears"
                        TO "CK_payroll_allowance_seniority_records_CompletedSeniorityYears";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_employee_seniority_allowances_CompletedSeniorityMonths'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_allowance_seniority_records_CompletedSeniorityMonths'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        RENAME CONSTRAINT "CK_payroll_employee_seniority_allowances_CompletedSeniorityMonths"
                        TO "CK_payroll_allowance_seniority_records_CompletedSeniorityMonths";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_employee_seniority_allowances_SalaryWorkDays'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_allowance_seniority_records_SalaryWorkDays'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        RENAME CONSTRAINT "CK_payroll_employee_seniority_allowances_SalaryWorkDays"
                        TO "CK_payroll_allowance_seniority_records_SalaryWorkDays";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_employee_seniority_allowances_AllowanceAmount'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_allowance_seniority_records_AllowanceAmount'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        RENAME CONSTRAINT "CK_payroll_employee_seniority_allowances_AllowanceAmount"
                        TO "CK_payroll_allowance_seniority_records_AllowanceAmount";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        RENAME CONSTRAINT "FK_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId"
                        TO "FK_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId";
                    END IF;
                END $$;

                ALTER INDEX IF EXISTS "UX_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId"
                RENAME TO "UX_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId";

                ALTER INDEX IF EXISTS "IX_payroll_employee_seniority_allowances_IsLocked"
                RENAME TO "IX_payroll_allowance_seniority_records_IsLocked";

                ALTER INDEX IF EXISTS "IX_payroll_employee_seniority_allowances_AppliedRuleKey"
                RENAME TO "IX_payroll_allowance_seniority_records_AppliedRuleKey";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public.payroll_employee_responsibility_allowances') IS NULL
                        AND to_regclass('public.payroll_allowance_responsibility_records') IS NOT NULL
                    THEN
                        ALTER TABLE public.payroll_allowance_responsibility_records
                        RENAME TO payroll_employee_responsibility_allowances;
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'PK_payroll_allowance_responsibility_records'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'PK_payroll_employee_responsibility_allowances'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_responsibility_allowances
                        RENAME CONSTRAINT "PK_payroll_allowance_responsibility_records"
                        TO "PK_payroll_employee_responsibility_allowances";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_responsibility_records_employees_EmployeeId'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_employee_responsibility_allowances_employees_EmployeeId'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_responsibility_allowances
                        RENAME CONSTRAINT "FK_payroll_allowance_responsibility_records_employees_EmployeeId"
                        TO "FK_payroll_employee_responsibility_allowances_employees_EmployeeId";
                    END IF;
                END $$;

                ALTER INDEX IF EXISTS "IX_payroll_allowance_responsibility_records_Year_Month"
                RENAME TO "IX_payroll_employee_responsibility_allowances_Year_Month";

                ALTER INDEX IF EXISTS "IX_payroll_allowance_responsibility_records_Year_Month_EmployeeCode"
                RENAME TO "IX_payroll_employee_responsibility_allowances_Year_Month_EmployeeCode";

                ALTER INDEX IF EXISTS "IX_payroll_allowance_responsibility_records_EmployeeId"
                RENAME TO "IX_payroll_employee_responsibility_allowances_EmployeeId";

                DO $$
                BEGIN
                    IF to_regclass('public.payroll_employee_seniority_allowances') IS NULL
                        AND to_regclass('public.payroll_allowance_seniority_records') IS NOT NULL
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        RENAME TO payroll_employee_seniority_allowances;
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'PK_payroll_allowance_seniority_records'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'PK_payroll_employee_seniority_allowances'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_seniority_allowances
                        RENAME CONSTRAINT "PK_payroll_allowance_seniority_records"
                        TO "PK_payroll_employee_seniority_allowances";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_allowance_seniority_records_CompletedSeniorityYears'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_employee_seniority_allowances_CompletedSeniorityYears'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_seniority_allowances
                        RENAME CONSTRAINT "CK_payroll_allowance_seniority_records_CompletedSeniorityYears"
                        TO "CK_payroll_employee_seniority_allowances_CompletedSeniorityYears";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_allowance_seniority_records_CompletedSeniorityMonths'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_employee_seniority_allowances_CompletedSeniorityMonths'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_seniority_allowances
                        RENAME CONSTRAINT "CK_payroll_allowance_seniority_records_CompletedSeniorityMonths"
                        TO "CK_payroll_employee_seniority_allowances_CompletedSeniorityMonths";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_allowance_seniority_records_SalaryWorkDays'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_employee_seniority_allowances_SalaryWorkDays'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_seniority_allowances
                        RENAME CONSTRAINT "CK_payroll_allowance_seniority_records_SalaryWorkDays"
                        TO "CK_payroll_employee_seniority_allowances_SalaryWorkDays";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_allowance_seniority_records_AllowanceAmount'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'CK_payroll_employee_seniority_allowances_AllowanceAmount'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_seniority_allowances
                        RENAME CONSTRAINT "CK_payroll_allowance_seniority_records_AllowanceAmount"
                        TO "CK_payroll_employee_seniority_allowances_AllowanceAmount";
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId'
                        )
                    THEN
                        ALTER TABLE public.payroll_employee_seniority_allowances
                        RENAME CONSTRAINT "FK_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId"
                        TO "FK_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId";
                    END IF;
                END $$;

                ALTER INDEX IF EXISTS "UX_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId"
                RENAME TO "UX_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId";

                ALTER INDEX IF EXISTS "IX_payroll_allowance_seniority_records_IsLocked"
                RENAME TO "IX_payroll_employee_seniority_allowances_IsLocked";

                ALTER INDEX IF EXISTS "IX_payroll_allowance_seniority_records_AppliedRuleKey"
                RENAME TO "IX_payroll_employee_seniority_allowances_AppliedRuleKey";
                """);
        }
    }
}
