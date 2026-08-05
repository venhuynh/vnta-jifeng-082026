using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHazardAllowanceExportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_hazard_allowance_export_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FilterJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    OutputPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_hazard_allowance_export_jobs", x => x.Id);
                    table.CheckConstraint("CK_payroll_hazard_allowance_export_jobs_Status", "\"Status\" IN (0, 1, 2, 3)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_hazard_allowance_export_jobs_RequestedBy_CreatedAtUtc",
                table: "payroll_hazard_allowance_export_jobs",
                columns: new[] { "RequestedBy", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_hazard_allowance_export_jobs_Status_CreatedAtUtc",
                table: "payroll_hazard_allowance_export_jobs",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_hazard_allowance_export_jobs");
        }
    }
}
