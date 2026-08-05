using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollMealAllowanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_meal_allowance_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollMonth = table.Column<short>(type: "smallint", nullable: false),
                    PayrollYear = table.Column<short>(type: "smallint", nullable: false),
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
                    table.PrimaryKey("PK_payroll_meal_allowance_records", x => x.Id);
                    table.CheckConstraint("CK_payroll_meal_allowance_records_ActualWorkdayCount", "\"ActualWorkdayCount\" >= 0");
                    table.CheckConstraint("CK_payroll_meal_allowance_records_AttendanceRate", "\"AttendanceRate\" >= 0 AND \"AttendanceRate\" <= 1");
                    table.CheckConstraint("CK_payroll_meal_allowance_records_PayrollMonth", "\"PayrollMonth\" >= 1 AND \"PayrollMonth\" <= 12");
                    table.CheckConstraint("CK_payroll_meal_allowance_records_StandardWorkdayCount", "\"StandardWorkdayCount\" > 0");
                    table.ForeignKey(
                        name: "FK_payroll_meal_allowance_records_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_meal_allowance_records_IsLocked",
                table: "payroll_meal_allowance_records",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_meal_allowance_records_PayrollYear_PayrollMonth",
                table: "payroll_meal_allowance_records",
                columns: new[] { "PayrollYear", "PayrollMonth" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_meal_allowance_records_EmployeeId_PayrollYear_PayrollMonth",
                table: "payroll_meal_allowance_records",
                columns: new[] { "EmployeeId", "PayrollYear", "PayrollMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_meal_allowance_records");
        }
    }
}
