using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollHazardAllowanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "RequireDocument",
                table: "attendance_workday_summaries",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsRegisterForOT",
                table: "attendance_workday_summaries",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsLocked",
                table: "attendance_workday_summaries",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.CreateTable(
                name: "payroll_hazard_allowance_records",
                columns: table => new
                {
                    PayrollAllowanceSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualifiedWorkdayCount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    LateEarlyDeductionDays = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false, defaultValue: 0m),
                    PayableWorkdayCount = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false, defaultValue: 0m),
                    HazardAllowancePerDay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    HazardAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsEligibleDepartment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExclusionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_hazard_allowance_records", x => x.PayrollAllowanceSummaryRecordId);
                    table.CheckConstraint("CK_payroll_hazard_allowance_records_HazardAllowanceAmount", "\"HazardAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_hazard_allowance_records_HazardAllowancePerDay", "\"HazardAllowancePerDay\" >= 0");
                    table.CheckConstraint("CK_payroll_hazard_allowance_records_LateEarlyDeductionDays", "\"LateEarlyDeductionDays\" >= 0");
                    table.CheckConstraint("CK_payroll_hazard_allowance_records_PayableWorkdayCount", "\"PayableWorkdayCount\" >= 0");
                    table.CheckConstraint("CK_payroll_hazard_allowance_records_QualifiedWorkdayCount", "\"QualifiedWorkdayCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_hazard_allowance_records_payroll_allowance_summary_~",
                        column: x => x.PayrollAllowanceSummaryRecordId,
                        principalTable: "payroll_allowance_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
            migrationBuilder.DropTable(
                name: "payroll_hazard_allowance_records");

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS ux_devices_serial_number_not_empty;
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "RequireDocument",
                table: "attendance_workday_summaries",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRegisterForOT",
                table: "attendance_workday_summaries",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLocked",
                table: "attendance_workday_summaries",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
