using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPayrollAllowanceHazardRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_hazard_allowance_records_payroll_allowance_summary_~",
                table: "payroll_hazard_allowance_records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_hazard_allowance_records",
                table: "payroll_hazard_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_HazardAllowanceAmount",
                table: "payroll_hazard_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_HazardAllowancePerDay",
                table: "payroll_hazard_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_LateEarlyDeductionDays",
                table: "payroll_hazard_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_PayableWorkdayCount",
                table: "payroll_hazard_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_QualifiedWorkdayCount",
                table: "payroll_hazard_allowance_records");

            migrationBuilder.RenameTable(
                name: "payroll_hazard_allowance_records",
                newName: "payroll_allowance_hazard_records");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "payroll_allowance_hazard_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE payroll_allowance_hazard_records
                SET "Id" = "PayrollAllowanceSummaryRecordId"
                WHERE "Id" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "payroll_allowance_hazard_records",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_allowance_hazard_records",
                table: "payroll_allowance_hazard_records",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_hazard_records_PayrollAllowanceSummaryRec~",
                table: "payroll_allowance_hazard_records",
                column: "PayrollAllowanceSummaryRecordId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_HazardAllowanceAmount",
                table: "payroll_allowance_hazard_records",
                sql: "\"HazardAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_HazardAllowancePerDay",
                table: "payroll_allowance_hazard_records",
                sql: "\"HazardAllowancePerDay\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_LateEarlyDeductionDays",
                table: "payroll_allowance_hazard_records",
                sql: "\"LateEarlyDeductionDays\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_PayableWorkdayCount",
                table: "payroll_allowance_hazard_records",
                sql: "\"PayableWorkdayCount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_QualifiedWorkdayCount",
                table: "payroll_allowance_hazard_records",
                sql: "\"QualifiedWorkdayCount\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_hazard_records_payroll_allowance_summary_~",
                table: "payroll_allowance_hazard_records",
                column: "PayrollAllowanceSummaryRecordId",
                principalTable: "payroll_allowance_summary_records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_hazard_records_payroll_allowance_summary_~",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_allowance_hazard_records",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.DropIndex(
                name: "IX_payroll_allowance_hazard_records_PayrollAllowanceSummaryRec~",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_HazardAllowanceAmount",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_HazardAllowancePerDay",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_LateEarlyDeductionDays",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_PayableWorkdayCount",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_hazard_records_QualifiedWorkdayCount",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "payroll_allowance_hazard_records");

            migrationBuilder.RenameTable(
                name: "payroll_allowance_hazard_records",
                newName: "payroll_hazard_allowance_records");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_hazard_allowance_records",
                table: "payroll_hazard_allowance_records",
                column: "PayrollAllowanceSummaryRecordId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_HazardAllowanceAmount",
                table: "payroll_hazard_allowance_records",
                sql: "\"HazardAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_HazardAllowancePerDay",
                table: "payroll_hazard_allowance_records",
                sql: "\"HazardAllowancePerDay\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_LateEarlyDeductionDays",
                table: "payroll_hazard_allowance_records",
                sql: "\"LateEarlyDeductionDays\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_PayableWorkdayCount",
                table: "payroll_hazard_allowance_records",
                sql: "\"PayableWorkdayCount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_hazard_allowance_records_QualifiedWorkdayCount",
                table: "payroll_hazard_allowance_records",
                sql: "\"QualifiedWorkdayCount\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_hazard_allowance_records_payroll_allowance_summary_~",
                table: "payroll_hazard_allowance_records",
                column: "PayrollAllowanceSummaryRecordId",
                principalTable: "payroll_allowance_summary_records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
