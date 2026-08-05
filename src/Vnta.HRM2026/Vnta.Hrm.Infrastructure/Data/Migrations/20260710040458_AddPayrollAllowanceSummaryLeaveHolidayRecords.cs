using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollAllowanceSummaryLeaveHolidayRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
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
                    IF to_regclass('public.payroll_allowance_seniority_records') IS NOT NULL
                        AND EXISTS (
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
                    IF to_regclass('public.payroll_allowance_seniority_records') IS NOT NULL
                        AND EXISTS (
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
                    IF to_regclass('public.payroll_allowance_seniority_records') IS NOT NULL
                        AND EXISTS (
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
                    IF to_regclass('public.payroll_allowance_seniority_records') IS NOT NULL
                        AND EXISTS (
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
                    IF to_regclass('public.payroll_allowance_seniority_records') IS NOT NULL
                        AND EXISTS (
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
                    IF to_regclass('public.payroll_allowance_seniority_records') IS NOT NULL
                        AND EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_employee_seniority_allowances_payroll_allowance_sum~'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_seniority_records_payroll_allowance_summa~'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        RENAME CONSTRAINT "FK_payroll_employee_seniority_allowances_payroll_allowance_sum~"
                        TO "FK_payroll_allowance_seniority_records_payroll_allowance_summa~";
                    END IF;
                END $$;

                ALTER INDEX IF EXISTS "UX_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId"
                    RENAME TO "UX_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId";

                ALTER INDEX IF EXISTS "IX_payroll_employee_seniority_allowances_IsLocked"
                    RENAME TO "IX_payroll_allowance_seniority_records_IsLocked";

                ALTER INDEX IF EXISTS "IX_payroll_employee_seniority_allowances_AppliedRuleKey"
                    RENAME TO "IX_payroll_allowance_seniority_records_AppliedRuleKey";

                CREATE TABLE IF NOT EXISTS public.payroll_allowance_seniority_records (
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
                    CONSTRAINT "PK_payroll_allowance_seniority_records" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_payroll_allowance_seniority_records_AllowanceAmount"
                        CHECK ("AllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_seniority_records_CompletedSeniorityMonths"
                        CHECK ("CompletedSeniorityMonths" IS NULL OR ("CompletedSeniorityMonths" >= 0 AND "CompletedSeniorityMonths" < 12)),
                    CONSTRAINT "CK_payroll_allowance_seniority_records_CompletedSeniorityYears"
                        CHECK ("CompletedSeniorityYears" IS NULL OR "CompletedSeniorityYears" >= 0),
                    CONSTRAINT "CK_payroll_allowance_seniority_records_SalaryWorkDays"
                        CHECK ("SalaryWorkDays" IS NULL OR "SalaryWorkDays" >= 0)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId"
                    ON public.payroll_allowance_seniority_records ("PayrollAllowanceSummaryRecordId");

                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_seniority_records_IsLocked"
                    ON public.payroll_allowance_seniority_records ("IsLocked");

                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_seniority_records_AppliedRuleKey"
                    ON public.payroll_allowance_seniority_records ("AppliedRuleKey");

                DO $$
                BEGIN
                    IF to_regclass('public.payroll_allowance_summary_records') IS NOT NULL
                        AND to_regclass('public.payroll_allowance_seniority_records') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_seniority_records_payroll_allowance_summa~'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_seniority_records
                        ADD CONSTRAINT "FK_payroll_allowance_seniority_records_payroll_allowance_summa~"
                        FOREIGN KEY ("PayrollAllowanceSummaryRecordId")
                        REFERENCES public.payroll_allowance_summary_records ("Id")
                        ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE TABLE IF NOT EXISTS public.payroll_allowance_attendance_records (
                    "Id" uuid NOT NULL,
                    "PayrollAllowanceSummaryRecordId" uuid NOT NULL,
                    "StandardAllowanceAmount" numeric(18,2) NOT NULL,
                    "StandardWorkdayCount" numeric(10,2) NOT NULL,
                    "ActualWorkdayCount" numeric(10,2) NOT NULL,
                    "AttendanceRate" numeric(7,4) NOT NULL,
                    "AllowanceAmount" numeric(18,2) NOT NULL,
                    "AppliedRuleKey" character varying(32) NULL,
                    "AttendanceClass" character varying(16) NULL,
                    "CtlWorkdayCount" numeric(10,2) NULL,
                    "LateEarlyMinutes" integer NULL,
                    "Kqcc" numeric(10,2) NULL,
                    "HasKpViolation" boolean NOT NULL DEFAULT FALSE,
                    "Note" text NULL,
                    "IsLocked" boolean NOT NULL DEFAULT FALSE,
                    "RefreshedAtUtc" timestamp without time zone NULL,
                    "RefreshedBy" character varying(128) NULL,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "CreatedBy" character varying(128) NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone NULL,
                    "UpdatedBy" character varying(128) NULL,
                    CONSTRAINT "PK_payroll_allowance_attendance_records" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_payroll_allowance_attendance_records_ActualWorkdayCount"
                        CHECK ("ActualWorkdayCount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_attendance_records_AllowanceAmount"
                        CHECK ("AllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_attendance_records_AttendanceRate"
                        CHECK ("AttendanceRate" >= 0 AND "AttendanceRate" <= 1),
                    CONSTRAINT "CK_payroll_allowance_attendance_records_CtlWorkdayCount"
                        CHECK ("CtlWorkdayCount" IS NULL OR "CtlWorkdayCount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_attendance_records_Kqcc"
                        CHECK ("Kqcc" IS NULL OR "Kqcc" >= 0),
                    CONSTRAINT "CK_payroll_allowance_attendance_records_LateEarlyMinutes"
                        CHECK ("LateEarlyMinutes" IS NULL OR "LateEarlyMinutes" >= 0),
                    CONSTRAINT "CK_payroll_allowance_attendance_records_StandardAllowanceAmount"
                        CHECK ("StandardAllowanceAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_attendance_records_StandardWorkdayCount"
                        CHECK ("StandardWorkdayCount" > 0)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_allowance_attendance_records_PayrollAllowanceSummaryRecordId"
                    ON public.payroll_allowance_attendance_records ("PayrollAllowanceSummaryRecordId");

                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_attendance_records_IsLocked"
                    ON public.payroll_allowance_attendance_records ("IsLocked");

                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_attendance_records_AppliedRuleKey"
                    ON public.payroll_allowance_attendance_records ("AppliedRuleKey");

                CREATE INDEX IF NOT EXISTS "IX_payroll_allowance_attendance_records_AttendanceClass"
                    ON public.payroll_allowance_attendance_records ("AttendanceClass");

                DO $$
                BEGIN
                    IF to_regclass('public.payroll_allowance_summary_records') IS NOT NULL
                        AND to_regclass('public.payroll_allowance_attendance_records') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_attendance_records_payroll_allowance_summ~'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_attendance_records
                        ADD CONSTRAINT "FK_payroll_allowance_attendance_records_payroll_allowance_summ~"
                        FOREIGN KEY ("PayrollAllowanceSummaryRecordId")
                        REFERENCES public.payroll_allowance_summary_records ("Id")
                        ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE TABLE IF NOT EXISTS public.payroll_allowance_summary_leave_holiday_records (
                    "PayrollAllowanceSummaryRecordId" uuid NOT NULL,
                    "DailyWageAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "LeaveDayCount" numeric(9,2) NOT NULL DEFAULT 0,
                    "HolidayDayCount" numeric(9,2) NOT NULL DEFAULT 0,
                    "LeaveHolidayAllowanceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                    "Note" text NULL,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "CreatedBy" character varying(128) NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone NULL,
                    "UpdatedBy" character varying(128) NULL,
                    CONSTRAINT "PK_payroll_allowance_summary_leave_holiday_records" PRIMARY KEY ("PayrollAllowanceSummaryRecordId"),
                    CONSTRAINT "CK_payroll_allowance_summary_leave_holiday_records_DailyWageAm~"
                        CHECK ("DailyWageAmount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_leave_holiday_records_HolidayDayC~"
                        CHECK ("HolidayDayCount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_leave_holiday_records_LeaveDayCou~"
                        CHECK ("LeaveDayCount" >= 0),
                    CONSTRAINT "CK_payroll_allowance_summary_leave_holiday_records_LeaveHolida~"
                        CHECK ("LeaveHolidayAllowanceAmount" >= 0)
                );

                DO $$
                BEGIN
                    IF to_regclass('public.payroll_allowance_summary_records') IS NOT NULL
                        AND to_regclass('public.payroll_allowance_summary_leave_holiday_records') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_payroll_allowance_summary_leave_holiday_records_payroll_all~'
                        )
                    THEN
                        ALTER TABLE public.payroll_allowance_summary_leave_holiday_records
                        ADD CONSTRAINT "FK_payroll_allowance_summary_leave_holiday_records_payroll_all~"
                        FOREIGN KEY ("PayrollAllowanceSummaryRecordId")
                        REFERENCES public.payroll_allowance_summary_records ("Id")
                        ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_seniority_records_payroll_allowance_summa~",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.DropTable(
                name: "payroll_allowance_attendance_records");

            migrationBuilder.DropTable(
                name: "payroll_allowance_summary_leave_holiday_records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_allowance_seniority_records",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_AllowanceAmount",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_CompletedSeniorityMonths",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_CompletedSeniorityYears",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_seniority_records_SalaryWorkDays",
                table: "payroll_allowance_seniority_records");

            migrationBuilder.RenameTable(
                name: "payroll_allowance_seniority_records",
                newName: "payroll_employee_seniority_allowances");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_allowance_seniority_records_PayrollAllowanceSummaryRecordId",
                table: "payroll_employee_seniority_allowances",
                newName: "UX_payroll_employee_seniority_allowances_PayrollAllowanceSummaryRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_seniority_records_IsLocked",
                table: "payroll_employee_seniority_allowances",
                newName: "IX_payroll_employee_seniority_allowances_IsLocked");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_seniority_records_AppliedRuleKey",
                table: "payroll_employee_seniority_allowances",
                newName: "IX_payroll_employee_seniority_allowances_AppliedRuleKey");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_employee_seniority_allowances",
                table: "payroll_employee_seniority_allowances",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_employee_seniority_allowances_AllowanceAmount",
                table: "payroll_employee_seniority_allowances",
                sql: "\"AllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_employee_seniority_allowances_CompletedSeniorityMon~",
                table: "payroll_employee_seniority_allowances",
                sql: "\"CompletedSeniorityMonths\" IS NULL OR (\"CompletedSeniorityMonths\" >= 0 AND \"CompletedSeniorityMonths\" < 12)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_employee_seniority_allowances_CompletedSeniorityYea~",
                table: "payroll_employee_seniority_allowances",
                sql: "\"CompletedSeniorityYears\" IS NULL OR \"CompletedSeniorityYears\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_employee_seniority_allowances_SalaryWorkDays",
                table: "payroll_employee_seniority_allowances",
                sql: "\"SalaryWorkDays\" IS NULL OR \"SalaryWorkDays\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_employee_seniority_allowances_payroll_allowance_sum~",
                table: "payroll_employee_seniority_allowances",
                column: "PayrollAllowanceSummaryRecordId",
                principalTable: "payroll_allowance_summary_records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
