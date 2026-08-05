using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsibilityOtherAllowanceAmountToSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ResponsibilityOtherAllowanceAmount",
                table: "payroll_allowance_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_summary_records_ResponsibilityOtherAllowa~",
                table: "payroll_allowance_summary_records",
                sql: "\"ResponsibilityOtherAllowanceAmount\" >= 0");

            // Trước khi tách cột, số từ bảng trách nhiệm khác được ghi nhầm vào OtherAllowanceAmount.
            migrationBuilder.Sql(
                """
                UPDATE payroll_allowance_summary_records AS summary
                SET "ResponsibilityOtherAllowanceAmount" = COALESCE(detail."ActualResponsibilityAllowanceAmount", 0)
                FROM payroll_allowance_other_responsibility_records AS detail
                WHERE detail."PayrollAllowanceSummaryRecordId" = summary."Id";
                """);

            // OtherAllowanceAmount chỉ còn là tổng các dòng phụ cấp khác chuyên biệt.
            migrationBuilder.Sql(
                """
                UPDATE payroll_allowance_summary_records AS summary
                SET "OtherAllowanceAmount" = COALESCE(
                    (
                        SELECT SUM(detail."AllowanceAmount")
                        FROM payroll_allowance_other AS detail
                        WHERE detail."PayrollAllowanceSummaryRecordId" = summary."Id"
                    ),
                    0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE payroll_allowance_summary_records
                SET "OtherAllowanceAmount" = "OtherAllowanceAmount" + "ResponsibilityOtherAllowanceAmount";
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_summary_records_ResponsibilityOtherAllowa~",
                table: "payroll_allowance_summary_records");

            migrationBuilder.DropColumn(
                name: "ResponsibilityOtherAllowanceAmount",
                table: "payroll_allowance_summary_records");
        }
    }
}
