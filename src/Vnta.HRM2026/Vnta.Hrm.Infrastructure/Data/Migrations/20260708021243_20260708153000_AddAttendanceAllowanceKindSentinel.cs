using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class _20260708153000_AddAttendanceAllowanceKindSentinel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS ux_devices_serial_number_not_empty
                ON devices ("SerialNumber")
                WHERE "SerialNumber" IS NOT NULL AND btrim("SerialNumber") <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS ux_devices_serial_number_not_empty;
                """);
        }
    }
}
