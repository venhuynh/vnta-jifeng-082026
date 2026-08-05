using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceWorkCalendarDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance_work_calendar_days",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DayType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_work_calendar_days", x => x.Id);
                    table.CheckConstraint("CK_attendance_work_calendar_days_DayType", "\"DayType\" IN ('Ngày nghỉ', 'Ngày lễ')");
                });

            migrationBuilder.Sql(
                """
                UPDATE attendance_workday_summaries
                SET "DayType" = 'Ngày thường'
                WHERE "DayType" = 'regular' OR btrim("DayType") = '';

                UPDATE attendance_workday_summaries
                SET "DayType" = 'Ngày nghỉ'
                WHERE "DayType" IN ('day_off', 'dayoff', 'off', 'rest_day');

                UPDATE attendance_workday_summaries
                SET "DayType" = 'Ngày lễ'
                WHERE "DayType" = 'holiday';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_attendance_workday_summaries_DayType",
                table: "attendance_workday_summaries",
                sql: "\"DayType\" IN ('Ngày thường', 'Ngày nghỉ', 'Ngày lễ')");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_work_calendar_days_WorkDate",
                table: "attendance_work_calendar_days",
                column: "WorkDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_attendance_workday_summaries_DayType",
                table: "attendance_workday_summaries");

            migrationBuilder.DropTable(
                name: "attendance_work_calendar_days");
        }
    }
}
