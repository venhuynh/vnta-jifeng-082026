using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignAttendanceWorkdaySummaryDefaultsAndDeviceSerialIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.attendance_workday_summaries
                ALTER COLUMN "RequireDocument" SET DEFAULT FALSE;

                ALTER TABLE IF EXISTS public.attendance_workday_summaries
                ALTER COLUMN "IsRegisterForOT" SET DEFAULT FALSE;

                ALTER TABLE IF EXISTS public.attendance_workday_summaries
                ALTER COLUMN "IsLocked" SET DEFAULT FALSE;

                CREATE UNIQUE INDEX IF NOT EXISTS "ux_devices_serial_number_not_empty"
                ON public.devices USING btree ("SerialNumber")
                WHERE ("SerialNumber" IS NOT NULL AND btrim(("SerialNumber")::text) <> ''::text);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "ux_devices_serial_number_not_empty";

                ALTER TABLE IF EXISTS public.attendance_workday_summaries
                ALTER COLUMN "RequireDocument" DROP DEFAULT;

                ALTER TABLE IF EXISTS public.attendance_workday_summaries
                ALTER COLUMN "IsRegisterForOT" DROP DEFAULT;

                ALTER TABLE IF EXISTS public.attendance_workday_summaries
                ALTER COLUMN "IsLocked" DROP DEFAULT;
                """);
        }
    }
}
