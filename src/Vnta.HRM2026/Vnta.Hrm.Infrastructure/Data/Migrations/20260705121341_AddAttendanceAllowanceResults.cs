using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAllowanceResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.attendance_allowance_results (
                    "Id" uuid NOT NULL,
                    "EmployeeId" uuid NOT NULL,
                    "PayrollMonth" smallint NOT NULL,
                    "PayrollYear" smallint NOT NULL,
                    "StandardAllowanceAmount" numeric(18,2) NOT NULL,
                    "StandardWorkdayCount" numeric(10,2) NOT NULL,
                    "ActualWorkdayCount" numeric(10,2) NOT NULL,
                    "AttendanceRate" numeric(7,4) NOT NULL,
                    "ActualAllowanceAmount" numeric(18,2) NOT NULL,
                    "IsLocked" boolean NOT NULL DEFAULT FALSE,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone NULL,
                    CONSTRAINT "PK_attendance_allowance_results" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_attendance_allowance_results_PayrollMonth"
                        CHECK ("PayrollMonth" >= 1 AND "PayrollMonth" <= 12),
                    CONSTRAINT "CK_attendance_allowance_results_StandardWorkdayCount"
                        CHECK ("StandardWorkdayCount" > 0),
                    CONSTRAINT "CK_attendance_allowance_results_ActualWorkdayCount"
                        CHECK ("ActualWorkdayCount" >= 0),
                    CONSTRAINT "CK_attendance_allowance_results_AttendanceRate"
                        CHECK ("AttendanceRate" >= 0 AND "AttendanceRate" <= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_attendance_allowance_results_EmployeeId_PayrollYear_PayrollMonth"
                ON public.attendance_allowance_results ("EmployeeId", "PayrollYear", "PayrollMonth");

                CREATE INDEX IF NOT EXISTS "IX_attendance_allowance_results_PayrollYear_PayrollMonth"
                ON public.attendance_allowance_results ("PayrollYear", "PayrollMonth");

                CREATE INDEX IF NOT EXISTS "IX_attendance_allowance_results_IsLocked"
                ON public.attendance_allowance_results ("IsLocked");

                DO $$
                BEGIN
                    IF to_regclass('public.employees') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_attendance_allowance_results_employees_EmployeeId'
                        )
                    THEN
                        ALTER TABLE public.attendance_allowance_results
                        ADD CONSTRAINT "FK_attendance_allowance_results_employees_EmployeeId"
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
                DROP TABLE IF EXISTS public.attendance_allowance_results;
                """);
        }
    }
}
