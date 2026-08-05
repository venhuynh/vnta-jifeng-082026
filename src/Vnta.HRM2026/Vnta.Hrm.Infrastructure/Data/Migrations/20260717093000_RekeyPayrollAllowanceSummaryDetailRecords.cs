using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260717093000_RekeyPayrollAllowanceSummaryDetailRecords")]
    public partial class RekeyPayrollAllowanceSummaryDetailRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            RekeyToSummaryPrimaryKey(
                migrationBuilder,
                tableName: "payroll_allowance_hazard_records",
                uniqueIndexName: "IX_payroll_allowance_hazard_records_PayrollAllowanceSummaryRecordId",
                foreignKeyName: "FK_payroll_allowance_hazard_records_PayrollAllowanceSummaryRecordId",
                deleteBehavior: "CASCADE");

            RekeyToSummaryPrimaryKey(
                migrationBuilder,
                tableName: "payroll_allowance_other_responsibility_records",
                uniqueIndexName: "UX_payroll_allowance_other_responsibility_records_PayrollAllowanceSummaryRecordId",
                foreignKeyName: "FK_payroll_allowance_other_responsibility_records_PayrollAllowanceSummaryRecordId",
                deleteBehavior: "RESTRICT");

            RekeyToSummaryPrimaryKey(
                migrationBuilder,
                tableName: "payroll_allowance_attendance_records",
                uniqueIndexName: "UX_payroll_allowance_attendance_records_PayrollAllowanceSummaryRecordId",
                foreignKeyName: "FK_payroll_allowance_attendance_records_PayrollAllowanceSummaryRecordId",
                deleteBehavior: "RESTRICT");

            RekeyToSummaryPrimaryKey(
                migrationBuilder,
                tableName: "payroll_allowance_seniority_records",
                uniqueIndexName: "UX_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId",
                foreignKeyName: "FK_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId",
                deleteBehavior: "RESTRICT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RestoreIndependentPrimaryKey(
                migrationBuilder,
                tableName: "payroll_allowance_hazard_records",
                uniqueIndexName: "IX_payroll_allowance_hazard_records_PayrollAllowanceSummaryRecordId",
                foreignKeyName: "FK_payroll_allowance_hazard_records_PayrollAllowanceSummaryRecordId",
                deleteBehavior: "CASCADE");

            RestoreIndependentPrimaryKey(
                migrationBuilder,
                tableName: "payroll_allowance_other_responsibility_records",
                uniqueIndexName: "UX_payroll_allowance_other_responsibility_records_PayrollAllowanceSummaryRecordId",
                foreignKeyName: "FK_payroll_allowance_other_responsibility_records_PayrollAllowanceSummaryRecordId",
                deleteBehavior: "RESTRICT");

            RestoreIndependentPrimaryKey(
                migrationBuilder,
                tableName: "payroll_allowance_attendance_records",
                uniqueIndexName: "UX_payroll_allowance_attendance_records_PayrollAllowanceSummaryRecordId",
                foreignKeyName: "FK_payroll_allowance_attendance_records_PayrollAllowanceSummaryRecordId",
                deleteBehavior: "RESTRICT");

            RestoreIndependentPrimaryKey(
                migrationBuilder,
                tableName: "payroll_allowance_seniority_records",
                uniqueIndexName: "UX_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId",
                foreignKeyName: "FK_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId",
                deleteBehavior: "RESTRICT");
        }

        private static void RekeyToSummaryPrimaryKey(
            MigrationBuilder migrationBuilder,
            string tableName,
            string uniqueIndexName,
            string foreignKeyName,
            string deleteBehavior)
        {
            migrationBuilder.Sql(
                $"""
                DROP INDEX IF EXISTS "{uniqueIndexName}";

                DO $$
                DECLARE
                    constraint_name text;
                BEGIN
                    IF to_regclass('public.{tableName}') IS NOT NULL THEN
                        FOR constraint_name IN
                            SELECT conname
                            FROM pg_constraint
                            WHERE conrelid = 'public.{tableName}'::regclass
                                AND contype = 'f'
                        LOOP
                            EXECUTE format('ALTER TABLE public.{tableName} DROP CONSTRAINT %I', constraint_name);
                        END LOOP;

                        FOR constraint_name IN
                            SELECT conname
                            FROM pg_constraint
                            WHERE conrelid = 'public.{tableName}'::regclass
                                AND contype = 'p'
                        LOOP
                            EXECUTE format('ALTER TABLE public.{tableName} DROP CONSTRAINT %I', constraint_name);
                        END LOOP;

                        IF EXISTS (
                                SELECT 1
                                FROM information_schema.columns
                                WHERE table_schema = 'public'
                                    AND table_name = '{tableName}'
                                    AND column_name = 'Id'
                            )
                        THEN
                            ALTER TABLE public.{tableName}
                            DROP COLUMN "Id";
                        END IF;

                        ALTER TABLE public.{tableName}
                        ADD CONSTRAINT "PK_{tableName}"
                        PRIMARY KEY ("PayrollAllowanceSummaryRecordId");

                        ALTER TABLE public.{tableName}
                        ADD CONSTRAINT "{foreignKeyName}"
                        FOREIGN KEY ("PayrollAllowanceSummaryRecordId")
                        REFERENCES public.payroll_allowance_summary_records ("Id")
                        ON DELETE {deleteBehavior};
                    END IF;
                END $$;
                """);
        }

        private static void RestoreIndependentPrimaryKey(
            MigrationBuilder migrationBuilder,
            string tableName,
            string uniqueIndexName,
            string foreignKeyName,
            string deleteBehavior)
        {
            migrationBuilder.Sql(
                $"""
                DO $$
                DECLARE
                    constraint_name text;
                BEGIN
                    IF to_regclass('public.{tableName}') IS NOT NULL THEN
                        FOR constraint_name IN
                            SELECT conname
                            FROM pg_constraint
                            WHERE conrelid = 'public.{tableName}'::regclass
                                AND contype = 'f'
                        LOOP
                            EXECUTE format('ALTER TABLE public.{tableName} DROP CONSTRAINT %I', constraint_name);
                        END LOOP;

                        FOR constraint_name IN
                            SELECT conname
                            FROM pg_constraint
                            WHERE conrelid = 'public.{tableName}'::regclass
                                AND contype = 'p'
                        LOOP
                            EXECUTE format('ALTER TABLE public.{tableName} DROP CONSTRAINT %I', constraint_name);
                        END LOOP;

                        IF NOT EXISTS (
                                SELECT 1
                                FROM information_schema.columns
                                WHERE table_schema = 'public'
                                    AND table_name = '{tableName}'
                                    AND column_name = 'Id'
                            )
                        THEN
                            ALTER TABLE public.{tableName}
                            ADD COLUMN "Id" uuid NULL;
                        END IF;

                        UPDATE public.{tableName}
                        SET "Id" = COALESCE("Id", "PayrollAllowanceSummaryRecordId");

                        ALTER TABLE public.{tableName}
                        ALTER COLUMN "Id" SET NOT NULL;

                        ALTER TABLE public.{tableName}
                        ADD CONSTRAINT "PK_{tableName}"
                        PRIMARY KEY ("Id");

                        ALTER TABLE public.{tableName}
                        ADD CONSTRAINT "{foreignKeyName}"
                        FOREIGN KEY ("PayrollAllowanceSummaryRecordId")
                        REFERENCES public.payroll_allowance_summary_records ("Id")
                        ON DELETE {deleteBehavior};
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "{uniqueIndexName}"
                    ON public.{tableName} ("PayrollAllowanceSummaryRecordId");
                """);
        }
    }
}
