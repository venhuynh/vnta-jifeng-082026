using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollFamilyDeductionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_decuction_family_deduction_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollDeductionSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependentCount = table.Column<short>(type: "smallint", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_decuction_family_deduction_records", x => x.Id);
                    table.CheckConstraint("CK_payroll_decuction_family_deduction_records_DependentCount", "\"DependentCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_decuction_family_deduction_records_payroll_decuctio~",
                        column: x => x.PayrollDeductionSummaryRecordId,
                        principalTable: "payroll_decuction_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_decuction_family_deduction_records_IsLocked",
                table: "payroll_decuction_family_deduction_records",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "UX_payroll_decuction_family_deduction_records_SummaryId",
                table: "payroll_decuction_family_deduction_records",
                column: "PayrollDeductionSummaryRecordId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_decuction_family_deduction_records");

        }
    }
}
