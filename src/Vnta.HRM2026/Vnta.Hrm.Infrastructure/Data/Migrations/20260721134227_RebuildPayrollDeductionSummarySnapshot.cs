using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebuildPayrollDeductionSummarySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceDeductionAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherDeductionAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PersonalIncomeTaxDeductionAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SocialInsuranceDeductionAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnionFeeDeductionAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_AdvanceDeductionAmount",
                table: "payroll_decuction_summary_records",
                sql: "\"AdvanceDeductionAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_OtherDeductionAmount",
                table: "payroll_decuction_summary_records",
                sql: "\"OtherDeductionAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_PersonalIncomeTaxDeductio~",
                table: "payroll_decuction_summary_records",
                sql: "\"PersonalIncomeTaxDeductionAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_SocialInsuranceDeductionA~",
                table: "payroll_decuction_summary_records",
                sql: "\"SocialInsuranceDeductionAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_UnionFeeDeductionAmount",
                table: "payroll_decuction_summary_records",
                sql: "\"UnionFeeDeductionAmount\" >= 0");

            // Snapshot deduction được tạo lại từ nguồn sau migration; purge cả
            // cụm parent-child để không giữ dữ liệu amount theo schema cũ.
            migrationBuilder.Sql(
                "TRUNCATE TABLE public.payroll_decuction_summary_records CASCADE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_AdvanceDeductionAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_OtherDeductionAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_PersonalIncomeTaxDeductio~",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_SocialInsuranceDeductionA~",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_UnionFeeDeductionAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "AdvanceDeductionAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "OtherDeductionAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "PersonalIncomeTaxDeductionAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "SocialInsuranceDeductionAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "UnionFeeDeductionAmount",
                table: "payroll_decuction_summary_records");

        }
    }
}
