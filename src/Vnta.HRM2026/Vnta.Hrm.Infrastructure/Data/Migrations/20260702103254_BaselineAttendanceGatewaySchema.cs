using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BaselineAttendanceGatewaySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline-only migration:
            // the attendance/gateway schema already exists in PostgreSQL.
            // This migration only advances EF migration history so
            // ApplicationDbContext can own the model without recreating tables.
            // New empty databases still need a separate bootstrap strategy
            // if they must create attendance/gateway tables from scratch.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
            // Rolling back this baseline should not drop pre-existing tables.
        }
    }
}
