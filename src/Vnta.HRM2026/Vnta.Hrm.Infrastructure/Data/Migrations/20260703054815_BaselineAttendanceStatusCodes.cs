using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BaselineAttendanceStatusCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline/no-op migration:
            // the runtime schema already contains `attendance_status_codes`
            // and `shifts`; this migration only updates the EF snapshot so
            // ApplicationDbContext can take ownership without recreating tables.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op by design. The existing schema remains owned by the database.
        }
    }
}
