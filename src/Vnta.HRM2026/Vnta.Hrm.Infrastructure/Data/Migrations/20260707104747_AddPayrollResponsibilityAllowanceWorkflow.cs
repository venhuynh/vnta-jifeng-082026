using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollResponsibilityAllowanceWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_monthly_responsibility_allowance_grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StandardResponsibilityAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_monthly_responsibility_allowance_grades", x => x.Id);
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_grades_DisplayOrder", "\"DisplayOrder\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_grades_Month", "\"Month\" BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_grades_StandardRes~", "\"StandardResponsibilityAllowanceAmount\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "payroll_monthly_responsibility_allowance_abc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PositionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: true),
                    GradeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GradeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    ActualWorkDays = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    StandardWorkDays = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    AbcRating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MonthlyPerformanceBonusAmount = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 0m),
                    IsPerformanceBonusExcluded = table.Column<bool>(type: "boolean", nullable: false),
                    StandardResponsibilityAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ActualResponsibilityAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CalculatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LockedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_monthly_responsibility_allowance_abc", x => x.Id);
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_abc_ActualResponsi~", "\"ActualResponsibilityAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_abc_ActualWorkDays", "\"ActualWorkDays\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_abc_Month", "\"Month\" BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_abc_MonthlyPerform~", "\"MonthlyPerformanceBonusAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_abc_StandardRespon~", "\"StandardResponsibilityAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_abc_StandardWorkDa~", "\"StandardWorkDays\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_monthly_responsibility_allowance_abc_employees_Empl~",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_monthly_responsibility_allowance_abc_payroll_monthl~",
                        column: x => x.GradeId,
                        principalTable: "payroll_monthly_responsibility_allowance_grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_monthly_responsibility_allowance_abc_positions_Posi~",
                        column: x => x.PositionId,
                        principalTable: "positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_monthly_responsibility_allowance_employee_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: true),
                    StandardResponsibilityAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AssignmentSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_monthly_responsibility_allowance_employee_assignmen~", x => x.Id);
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_employee_assignme~1", "\"StandardResponsibilityAllowanceAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_employee_assignmen~", "\"Month\" BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_payroll_monthly_responsibility_allowance_employee_assignmen~",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_monthly_responsibility_allowance_employee_assignme~1",
                        column: x => x.GradeId,
                        principalTable: "payroll_monthly_responsibility_allowance_grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_monthly_responsibility_allowance_grade_positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_monthly_responsibility_allowance_grade_positions", x => x.Id);
                    table.CheckConstraint("CK_payroll_monthly_responsibility_allowance_grade_positions_Mo~", "\"Month\" BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_payroll_monthly_responsibility_allowance_grade_positions_pa~",
                        column: x => x.GradeId,
                        principalTable: "payroll_monthly_responsibility_allowance_grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_monthly_responsibility_allowance_grade_positions_po~",
                        column: x => x.PositionId,
                        principalTable: "positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_EmployeeId",
                table: "payroll_monthly_responsibility_allowance_abc",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_GradeId",
                table: "payroll_monthly_responsibility_allowance_abc",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_PositionId",
                table: "payroll_monthly_responsibility_allowance_abc",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_DepartmentName",
                table: "payroll_monthly_responsibility_allowance_abc",
                columns: new[] { "Year", "Month", "DepartmentName" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_EmployeeCode",
                table: "payroll_monthly_responsibility_allowance_abc",
                columns: new[] { "Year", "Month", "EmployeeCode" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_IsLocked",
                table: "payroll_monthly_responsibility_allowance_abc",
                columns: new[] { "Year", "Month", "IsLocked" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_monthly_responsibility_allowance_abc_Year_Month_EmployeeId",
                table: "payroll_monthly_responsibility_allowance_abc",
                columns: new[] { "Year", "Month", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_employee_assignme~1",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_employee_assignmen~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_employee_assignments_Year_Month_GradeId",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                columns: new[] { "Year", "Month", "GradeId" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_monthly_responsibility_allowance_employee_assignments_Year_Month_EmployeeId",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                columns: new[] { "Year", "Month", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_grade_positions_Gr~",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_grade_positions_Po~",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_grade_positions_Year_Month_GradeId",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                columns: new[] { "Year", "Month", "GradeId" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_monthly_responsibility_allowance_grade_positions_Year_Month_PositionId",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                columns: new[] { "Year", "Month", "PositionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_responsibility_allowance_grades_Year_Month_DisplayOrder_Code",
                table: "payroll_monthly_responsibility_allowance_grades",
                columns: new[] { "Year", "Month", "DisplayOrder", "Code" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_monthly_responsibility_allowance_grades_Year_Month_Code",
                table: "payroll_monthly_responsibility_allowance_grades",
                columns: new[] { "Year", "Month", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropTable(
                name: "payroll_monthly_responsibility_allowance_employee_assignments");

            migrationBuilder.DropTable(
                name: "payroll_monthly_responsibility_allowance_grade_positions");

            migrationBuilder.DropTable(
                name: "payroll_monthly_responsibility_allowance_grades");
        }
    }
}
