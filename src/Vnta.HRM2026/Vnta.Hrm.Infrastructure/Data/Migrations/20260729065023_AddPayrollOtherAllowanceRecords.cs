using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollOtherAllowanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_allowance_other",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollAllowanceSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowanceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsFixedAmount = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowanceAmount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false, defaultValue: 0m),
                    Note = table.Column<string>(type: "text", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_allowance_other", x => x.Id);
                    table.CheckConstraint("CK_payroll_allowance_other_AllowanceAmount", "\"AllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_allowance_other_NonFixedAmountIsZero", "\"IsFixedAmount\" OR \"AllowanceAmount\" = 0");
                    table.ForeignKey(
                        name: "FK_payroll_allowance_other_payroll_allowance_summary_records_P~",
                        column: x => x.PayrollAllowanceSummaryRecordId,
                        principalTable: "payroll_allowance_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_other_IsLocked",
                table: "payroll_allowance_other",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_other_PayrollAllowanceSummaryRecordId",
                table: "payroll_allowance_other",
                column: "PayrollAllowanceSummaryRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_allowance_other");
        }
    }
}
