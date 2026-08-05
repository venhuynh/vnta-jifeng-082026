using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollAllowanceOtherResponsibilityRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_allowance_other_responsibility_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollAllowanceSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowanceWorkdayCount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    StandardResponsibilityAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ActualResponsibilityAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Note = table.Column<string>(type: "text", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RefreshedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RefreshedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_allowance_other_responsibility_records", x => x.Id);
                    table.CheckConstraint("CK_payroll_allowance_other_responsibility_records_ActualRespon~", "\"ActualResponsibilityAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_allowance_other_responsibility_records_AllowanceWor~", "\"AllowanceWorkdayCount\" >= 0");
                    table.CheckConstraint("CK_payroll_allowance_other_responsibility_records_StandardResp~", "\"StandardResponsibilityAllowanceAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_allowance_other_responsibility_records_payroll_allo~",
                        column: x => x.PayrollAllowanceSummaryRecordId,
                        principalTable: "payroll_allowance_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_other_responsibility_records_IsLocked",
                table: "payroll_allowance_other_responsibility_records",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "UX_payroll_allowance_other_responsibility_records_PayrollAllowanceSummaryRecordId",
                table: "payroll_allowance_other_responsibility_records",
                column: "PayrollAllowanceSummaryRecordId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_allowance_other_responsibility_records");
        }
    }
}
