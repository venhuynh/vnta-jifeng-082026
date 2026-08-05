using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class SynchronizePendingPayrollSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The affected payroll and employee-personal tables are already present
        // in databases upgraded through the historical migrations. This marker
        // synchronizes EF's model snapshot without replaying obsolete DDL.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration intentionally changes no database objects.
    }
}
