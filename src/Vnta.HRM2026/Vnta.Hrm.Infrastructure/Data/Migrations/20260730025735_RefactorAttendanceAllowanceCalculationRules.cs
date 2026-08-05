using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAttendanceAllowanceCalculationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_attendance_records_ActualWorkdayCount",
                table: "payroll_allowance_attendance_records");
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_attendance_records_CtlWorkdayCount",
                table: "payroll_allowance_attendance_records");
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_attendance_records_Kqcc",
                table: "payroll_allowance_attendance_records");
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_attendance_records_StandardWorkdayCount",
                table: "payroll_allowance_attendance_records");

            migrationBuilder.AddColumn<decimal>(
                name: "AdministrativeWorkdayCount",
                table: "payroll_allowance_attendance_records",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(
                name: "LateEarlyDeductionDays",
                table: "payroll_allowance_attendance_records",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(name: "ActualWorkdayCount", table: "payroll_allowance_attendance_records", type: "numeric(10,4)", precision: 10, scale: 4, nullable: false, oldClrType: typeof(decimal), oldType: "numeric(10,2)", oldPrecision: 10, oldScale: 2);
            migrationBuilder.AlterColumn<decimal>(name: "StandardWorkdayCount", table: "payroll_allowance_attendance_records", type: "numeric(10,4)", precision: 10, scale: 4, nullable: false, oldClrType: typeof(decimal), oldType: "numeric(10,2)", oldPrecision: 10, oldScale: 2);
            migrationBuilder.AlterColumn<decimal>(name: "CtlWorkdayCount", table: "payroll_allowance_attendance_records", type: "numeric(10,4)", precision: 10, scale: 4, nullable: true, oldClrType: typeof(decimal), oldType: "numeric(10,2)", oldPrecision: 10, oldScale: 2, oldNullable: true);
            migrationBuilder.AlterColumn<decimal>(name: "Kqcc", table: "payroll_allowance_attendance_records", type: "numeric(10,4)", precision: 10, scale: 4, nullable: true, oldClrType: typeof(decimal), oldType: "numeric(10,2)", oldPrecision: 10, oldScale: 2, oldNullable: true);

            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_ActualWorkdayCount", table: "payroll_allowance_attendance_records", sql: "TRUE");
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_CtlWorkdayCount", table: "payroll_allowance_attendance_records", sql: "TRUE");
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_Kqcc", table: "payroll_allowance_attendance_records", sql: "TRUE");
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_StandardWorkdayCount", table: "payroll_allowance_attendance_records", sql: "\"StandardWorkdayCount\" >= 0");
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_AdministrativeWorkdayCount", table: "payroll_allowance_attendance_records", sql: "\"AdministrativeWorkdayCount\" >= 0");
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_LateEarlyDeductionDays", table: "payroll_allowance_attendance_records", sql: "\"LateEarlyDeductionDays\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_payroll_allowance_attendance_records_AdministrativeWorkdayCount", table: "payroll_allowance_attendance_records");
            migrationBuilder.DropCheckConstraint(name: "CK_payroll_allowance_attendance_records_LateEarlyDeductionDays", table: "payroll_allowance_attendance_records");
            migrationBuilder.DropCheckConstraint(name: "CK_payroll_allowance_attendance_records_ActualWorkdayCount", table: "payroll_allowance_attendance_records");
            migrationBuilder.DropCheckConstraint(name: "CK_payroll_allowance_attendance_records_CtlWorkdayCount", table: "payroll_allowance_attendance_records");
            migrationBuilder.DropCheckConstraint(name: "CK_payroll_allowance_attendance_records_Kqcc", table: "payroll_allowance_attendance_records");
            migrationBuilder.DropCheckConstraint(name: "CK_payroll_allowance_attendance_records_StandardWorkdayCount", table: "payroll_allowance_attendance_records");
            migrationBuilder.DropColumn(name: "AdministrativeWorkdayCount", table: "payroll_allowance_attendance_records");
            migrationBuilder.DropColumn(name: "LateEarlyDeductionDays", table: "payroll_allowance_attendance_records");
            migrationBuilder.AlterColumn<decimal>(name: "ActualWorkdayCount", table: "payroll_allowance_attendance_records", type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, oldClrType: typeof(decimal), oldType: "numeric(10,4)", oldPrecision: 10, oldScale: 4);
            migrationBuilder.AlterColumn<decimal>(name: "StandardWorkdayCount", table: "payroll_allowance_attendance_records", type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, oldClrType: typeof(decimal), oldType: "numeric(10,4)", oldPrecision: 10, oldScale: 4);
            migrationBuilder.AlterColumn<decimal>(name: "CtlWorkdayCount", table: "payroll_allowance_attendance_records", type: "numeric(10,2)", precision: 10, scale: 2, nullable: true, oldClrType: typeof(decimal), oldType: "numeric(10,4)", oldPrecision: 10, oldScale: 4, oldNullable: true);
            migrationBuilder.AlterColumn<decimal>(name: "Kqcc", table: "payroll_allowance_attendance_records", type: "numeric(10,2)", precision: 10, scale: 2, nullable: true, oldClrType: typeof(decimal), oldType: "numeric(10,4)", oldPrecision: 10, oldScale: 4, oldNullable: true);
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_ActualWorkdayCount", table: "payroll_allowance_attendance_records", sql: "\"ActualWorkdayCount\" >= 0");
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_CtlWorkdayCount", table: "payroll_allowance_attendance_records", sql: "\"CtlWorkdayCount\" IS NULL OR \"CtlWorkdayCount\" >= 0");
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_Kqcc", table: "payroll_allowance_attendance_records", sql: "\"Kqcc\" IS NULL OR \"Kqcc\" >= 0");
            migrationBuilder.AddCheckConstraint(name: "CK_payroll_allowance_attendance_records_StandardWorkdayCount", table: "payroll_allowance_attendance_records", sql: "\"StandardWorkdayCount\" > 0");
        }
    }
}
