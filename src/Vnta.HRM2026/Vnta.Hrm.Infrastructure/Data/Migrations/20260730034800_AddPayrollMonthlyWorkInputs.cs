using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollMonthlyWorkInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_monthly_work_inputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollYear = table.Column<short>(type: "smallint", nullable: false),
                    PayrollMonth = table.Column<short>(type: "smallint", nullable: false),
                    AdministrativeWorkDays = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 0m),
                    LateEarlyLeaveMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    OvertimeMinutes15 = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    OvertimeMinutes20 = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    OvertimeMinutes30 = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PayrollWorkDays = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 0m),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_monthly_work_inputs", x => x.Id);
                    table.CheckConstraint("CK_payroll_monthly_work_inputs_AdministrativeWorkDays", "\"AdministrativeWorkDays\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_work_inputs_LateEarlyLeaveMinutes", "\"LateEarlyLeaveMinutes\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_work_inputs_OvertimeMinutes15", "\"OvertimeMinutes15\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_work_inputs_OvertimeMinutes20", "\"OvertimeMinutes20\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_work_inputs_OvertimeMinutes30", "\"OvertimeMinutes30\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_work_inputs_PayrollMonth", "\"PayrollMonth\" BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_payroll_monthly_work_inputs_PayrollWorkDays", "\"PayrollWorkDays\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_work_inputs_PayrollYear", "\"PayrollYear\" BETWEEN 1 AND 9999");
                    table.ForeignKey(
                        name: "FK_payroll_monthly_work_inputs_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_work_inputs_IsLocked",
                table: "payroll_monthly_work_inputs",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_work_inputs_PayrollYear_PayrollMonth",
                table: "payroll_monthly_work_inputs",
                columns: new[] { "PayrollYear", "PayrollMonth" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_monthly_work_inputs_EmployeeId_PayrollYear_PayrollMonth",
                table: "payroll_monthly_work_inputs",
                columns: new[] { "EmployeeId", "PayrollYear", "PayrollMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_monthly_work_inputs");
        }
    }
}
