using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceWorkdaySummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS attendance_workday_summaries (
                    "Id" uuid NOT NULL,
                    "EmployeeId" uuid NOT NULL,
                    "WorkDate" date NOT NULL,
                    "DayType" character varying(50) NOT NULL,
                    "ShiftId" uuid,
                    "ScheduledStartAt" character varying(20),
                    "ScheduledEndAt" character varying(20),
                    "CheckInAt" character varying(20),
                    "CheckOutAt" character varying(20),
                    "LateMinutes" integer NOT NULL,
                    "EarlyLeaveMinutes" integer NOT NULL,
                    "ComputedAtUtc" timestamp without time zone NOT NULL,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone,
                    "Note" text,
                    "Status" character varying(50) NOT NULL,
                    "IsLocked" boolean NOT NULL DEFAULT FALSE,
                    "OvertimeMinutes" integer NOT NULL,
                    "OvertimeMinutes15" integer NOT NULL,
                    "OvertimeMinutes20" integer NOT NULL,
                    "OvertimeMinutes30" integer NOT NULL,
                    "CheckInForOT15" character varying(20),
                    "IsRegisterForOT" boolean NOT NULL DEFAULT FALSE,
                    "RequireDocument" boolean NOT NULL DEFAULT FALSE,
                    CONSTRAINT "PK_attendance_workday_summaries" PRIMARY KEY ("Id")
                );

                CREATE INDEX IF NOT EXISTS "IX_attendance_workday_summaries_EmployeeId"
                    ON attendance_workday_summaries ("EmployeeId");

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_attendance_workday_summaries_EmployeeId_WorkDate"
                    ON attendance_workday_summaries ("EmployeeId", "WorkDate");

                CREATE INDEX IF NOT EXISTS "IX_attendance_workday_summaries_ShiftId"
                    ON attendance_workday_summaries ("ShiftId");

                CREATE INDEX IF NOT EXISTS "IX_attendance_workday_summaries_WorkDate"
                    ON attendance_workday_summaries ("WorkDate");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_attendance_workday_summaries_employees_EmployeeId'
                    ) THEN
                        ALTER TABLE attendance_workday_summaries
                        ADD CONSTRAINT "FK_attendance_workday_summaries_employees_EmployeeId"
                        FOREIGN KEY ("EmployeeId") REFERENCES employees ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_attendance_workday_summaries_shifts_ShiftId'
                    ) THEN
                        ALTER TABLE attendance_workday_summaries
                        ADD CONSTRAINT "FK_attendance_workday_summaries_shifts_ShiftId"
                        FOREIGN KEY ("ShiftId") REFERENCES shifts ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_workday_summaries");
        }
    }
}
