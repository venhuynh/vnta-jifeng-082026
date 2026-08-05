using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePayrollDeductionSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder is null)
            {
            migrationBuilder.DropTable(
                name: "payroll_decuction_summary_insurance_details");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_BhxhYtAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_CongDoanAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_KhacAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_TamUngAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_summary_records_ThueTncnAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "BhxhYtAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "CongDoanAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "KhacAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "TamUngAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.DropColumn(
                name: "ThueTncnAmount",
                table: "payroll_decuction_summary_records");

            migrationBuilder.CreateTable(
                name: "payroll_decuction_advance_records",
                columns: table => new
                {
                    PayrollDeductionSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_decuction_advance_records", x => x.PayrollDeductionSummaryRecordId);
                    table.CheckConstraint("CK_payroll_decuction_advance_records_DeductionAmount", "\"DeductionAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_decuction_advance_records_payroll_decuction_summary~",
                        column: x => x.PayrollDeductionSummaryRecordId,
                        principalTable: "payroll_decuction_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_decuction_insurance_records",
                columns: table => new
                {
                    PayrollDeductionSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StandardWorkdayCount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ActualWorkdayCount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AttendanceRate = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    ActualAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_decuction_insurance_records", x => x.PayrollDeductionSummaryRecordId);
                    table.CheckConstraint("CK_payroll_decuction_insurance_records_ActualAllowanceAmount", "\"ActualAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_decuction_insurance_records_ActualWorkdayCount", "\"ActualWorkdayCount\" >= 0");
                    table.CheckConstraint("CK_payroll_decuction_insurance_records_AttendanceRate", "\"AttendanceRate\" >= 0 AND \"AttendanceRate\" <= 1");
                    table.CheckConstraint("CK_payroll_decuction_insurance_records_StandardAllowanceAmount", "\"StandardAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_decuction_insurance_records_StandardWorkdayCount", "\"StandardWorkdayCount\" > 0");
                    table.ForeignKey(
                        name: "FK_payroll_decuction_insurance_records_payroll_decuction_summa~",
                        column: x => x.PayrollDeductionSummaryRecordId,
                        principalTable: "payroll_decuction_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_decuction_other_records",
                columns: table => new
                {
                    PayrollDeductionSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_decuction_other_records", x => x.PayrollDeductionSummaryRecordId);
                    table.CheckConstraint("CK_payroll_decuction_other_records_DeductionAmount", "\"DeductionAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_decuction_other_records_payroll_decuction_summary_r~",
                        column: x => x.PayrollDeductionSummaryRecordId,
                        principalTable: "payroll_decuction_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_decuction_tax_records",
                columns: table => new
                {
                    PayrollDeductionSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_decuction_tax_records", x => x.PayrollDeductionSummaryRecordId);
                    table.CheckConstraint("CK_payroll_decuction_tax_records_DeductionAmount", "\"DeductionAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_decuction_tax_records_payroll_decuction_summary_rec~",
                        column: x => x.PayrollDeductionSummaryRecordId,
                        principalTable: "payroll_decuction_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_decuction_union_fee_records",
                columns: table => new
                {
                    PayrollDeductionSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_decuction_union_fee_records", x => x.PayrollDeductionSummaryRecordId);
                    table.CheckConstraint("CK_payroll_decuction_union_fee_records_DeductionAmount", "\"DeductionAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_decuction_union_fee_records_payroll_decuction_summa~",
                        column: x => x.PayrollDeductionSummaryRecordId,
                        principalTable: "payroll_decuction_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_decuction_advance_records_IsLocked",
                table: "payroll_decuction_advance_records",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_decuction_insurance_records_IsLocked",
                table: "payroll_decuction_insurance_records",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_decuction_other_records_IsLocked",
                table: "payroll_decuction_other_records",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_decuction_tax_records_IsLocked",
                table: "payroll_decuction_tax_records",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_decuction_union_fee_records_IsLocked",
                table: "payroll_decuction_union_fee_records",
                column: "IsLocked");
        }

            }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder is null)
            {
            migrationBuilder.DropTable(
                name: "payroll_decuction_advance_records");

            migrationBuilder.DropTable(
                name: "payroll_decuction_insurance_records");

            migrationBuilder.DropTable(
                name: "payroll_decuction_other_records");

            migrationBuilder.DropTable(
                name: "payroll_decuction_tax_records");

            migrationBuilder.DropTable(
                name: "payroll_decuction_union_fee_records");

            migrationBuilder.AddColumn<decimal>(
                name: "BhxhYtAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CongDoanAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KhacAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TamUngAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThueTncnAmount",
                table: "payroll_decuction_summary_records",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "payroll_decuction_summary_insurance_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualWorkdayCount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AttendanceRate = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PayrollDeductionSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StandardWorkdayCount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_decuction_summary_insurance_details", x => x.Id);
                    table.CheckConstraint("CK_payroll_decuction_summary_insurance_details_ActualAllowance~", "\"ActualAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_decuction_summary_insurance_details_ActualWorkdayCo~", "\"ActualWorkdayCount\" >= 0");
                    table.CheckConstraint("CK_payroll_decuction_summary_insurance_details_AttendanceRate", "\"AttendanceRate\" >= 0 AND \"AttendanceRate\" <= 1");
                    table.CheckConstraint("CK_payroll_decuction_summary_insurance_details_StandardAllowan~", "\"StandardAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_decuction_summary_insurance_details_StandardWorkday~", "\"StandardWorkdayCount\" > 0");
                    table.ForeignKey(
                        name: "FK_payroll_decuction_summary_insurance_details_payroll_decucti~",
                        column: x => x.PayrollDeductionSummaryRecordId,
                        principalTable: "payroll_decuction_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_BhxhYtAmount",
                table: "payroll_decuction_summary_records",
                sql: "\"BhxhYtAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_CongDoanAmount",
                table: "payroll_decuction_summary_records",
                sql: "\"CongDoanAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_KhacAmount",
                table: "payroll_decuction_summary_records",
                sql: "\"KhacAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_TamUngAmount",
                table: "payroll_decuction_summary_records",
                sql: "\"TamUngAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_decuction_summary_records_ThueTncnAmount",
                table: "payroll_decuction_summary_records",
                sql: "\"ThueTncnAmount\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_decuction_summary_insurance_details_IsLocked",
                table: "payroll_decuction_summary_insurance_details",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "UX_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId",
                table: "payroll_decuction_summary_insurance_details",
                column: "PayrollDeductionSummaryRecordId",
                unique: true);
            }
        }
    }
}
