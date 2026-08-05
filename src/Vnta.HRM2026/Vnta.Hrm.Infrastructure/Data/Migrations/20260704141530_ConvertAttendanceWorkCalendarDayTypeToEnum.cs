using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260704141530_ConvertAttendanceWorkCalendarDayTypeToEnum")]
    public partial class ConvertAttendanceWorkCalendarDayTypeToEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.attendance_work_calendar_days
                DROP CONSTRAINT IF EXISTS "CK_attendance_work_calendar_days_DayType";

                ALTER TABLE public.attendance_work_calendar_days
                ALTER COLUMN "DayType" TYPE smallint
                USING CASE
                    WHEN "DayType"::text IN ('1', 'DayOff', 'day_off', 'dayoff', 'off', 'rest_day', 'Ngay nghi', 'Ngày nghỉ') THEN 1
                    WHEN "DayType"::text IN ('2', 'Holiday', 'holiday', 'Ngay le', 'Ngày lễ') THEN 2
                    ELSE 1
                END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_work_calendar_days_DayType",
                table: "attendance_work_calendar_days",
                sql: "\"DayType\" IN (1, 2)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_attendance_work_calendar_days_DayType",
                table: "attendance_work_calendar_days");

            migrationBuilder.Sql(
                """
                ALTER TABLE public.attendance_work_calendar_days
                ALTER COLUMN "DayType" TYPE character varying(50)
                USING CASE
                    WHEN "DayType" = 1 THEN 'Ngày nghỉ'
                    WHEN "DayType" = 2 THEN 'Ngày lễ'
                    ELSE 'Ngày nghỉ'
                END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_work_calendar_days_DayType",
                table: "attendance_work_calendar_days",
                sql: "\"DayType\" IN ('Ngày nghỉ', 'Ngày lễ')");
        }
    }
}
