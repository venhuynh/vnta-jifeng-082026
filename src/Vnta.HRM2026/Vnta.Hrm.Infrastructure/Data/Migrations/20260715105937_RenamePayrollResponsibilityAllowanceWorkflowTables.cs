using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamePayrollResponsibilityAllowanceWorkflowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_abc_employees_Empl~",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_abc_payroll_monthl~",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_abc_positions_Posi~",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_employee_assignmen~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_employee_assignme~1",
                table: "payroll_monthly_responsibility_allowance_employee_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_grade_positions_pa~",
                table: "payroll_monthly_responsibility_allowance_grade_positions");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_grade_positions_po~",
                table: "payroll_monthly_responsibility_allowance_grade_positions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_monthly_responsibility_allowance_grades",
                table: "payroll_monthly_responsibility_allowance_grades");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_grades_DisplayOrder",
                table: "payroll_monthly_responsibility_allowance_grades");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_grades_Month",
                table: "payroll_monthly_responsibility_allowance_grades");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_grades_StandardRes~",
                table: "payroll_monthly_responsibility_allowance_grades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_monthly_responsibility_allowance_grade_positions",
                table: "payroll_monthly_responsibility_allowance_grade_positions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_grade_positions_Mo~",
                table: "payroll_monthly_responsibility_allowance_grade_positions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_monthly_responsibility_allowance_employee_assignmen~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_employee_assignme~1",
                table: "payroll_monthly_responsibility_allowance_employee_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_employee_assignmen~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_monthly_responsibility_allowance_abc",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_ActualResponsi~",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_ActualWorkDays",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_Month",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_MonthlyPerform~",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_StandardRespon~",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_StandardWorkDa~",
                table: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.RenameTable(
                name: "payroll_monthly_responsibility_allowance_grades",
                newName: "payroll_allowance_responsibility_grade");

            migrationBuilder.RenameTable(
                name: "payroll_monthly_responsibility_allowance_grade_positions",
                newName: "payroll_allowance_responsibility_grade_positions");

            migrationBuilder.RenameTable(
                name: "payroll_monthly_responsibility_allowance_employee_assignments",
                newName: "payroll_allowance_responsibility_employee_assignments");

            migrationBuilder.RenameTable(
                name: "payroll_monthly_responsibility_allowance_abc",
                newName: "payroll_allowance_responsibility_abc");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_monthly_responsibility_allowance_grades_Year_Month_Code",
                table: "payroll_allowance_responsibility_grade",
                newName: "UX_payroll_allowance_responsibility_grade_Year_Month_Code");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_grades_Year_Month_DisplayOrder_Code",
                table: "payroll_allowance_responsibility_grade",
                newName: "IX_payroll_allowance_responsibility_grade_Year_Month_DisplayOrder_Code");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_monthly_responsibility_allowance_grade_positions_Year_Month_PositionId",
                table: "payroll_allowance_responsibility_grade_positions",
                newName: "UX_payroll_allowance_responsibility_grade_positions_Year_Month_PositionId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_grade_positions_Year_Month_GradeId",
                table: "payroll_allowance_responsibility_grade_positions",
                newName: "IX_payroll_allowance_responsibility_grade_positions_Year_Month_GradeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_grade_positions_Po~",
                table: "payroll_allowance_responsibility_grade_positions",
                newName: "IX_payroll_allowance_responsibility_grade_positions_PositionId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_grade_positions_Gr~",
                table: "payroll_allowance_responsibility_grade_positions",
                newName: "IX_payroll_allowance_responsibility_grade_positions_GradeId");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_monthly_responsibility_allowance_employee_assignments_Year_Month_EmployeeId",
                table: "payroll_allowance_responsibility_employee_assignments",
                newName: "UX_payroll_allowance_responsibility_employee_assignments_Year_Month_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_employee_assignments_Year_Month_GradeId",
                table: "payroll_allowance_responsibility_employee_assignments",
                newName: "IX_payroll_allowance_responsibility_employee_assignments_Year_Month_GradeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_employee_assignmen~",
                table: "payroll_allowance_responsibility_employee_assignments",
                newName: "IX_payroll_allowance_responsibility_employee_assignments_Emplo~");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_employee_assignme~1",
                table: "payroll_allowance_responsibility_employee_assignments",
                newName: "IX_payroll_allowance_responsibility_employee_assignments_Grade~");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_monthly_responsibility_allowance_abc_Year_Month_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                newName: "UX_payroll_allowance_responsibility_abc_Year_Month_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_IsLocked",
                table: "payroll_allowance_responsibility_abc",
                newName: "IX_payroll_allowance_responsibility_abc_Year_Month_IsLocked");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_EmployeeCode",
                table: "payroll_allowance_responsibility_abc",
                newName: "IX_payroll_allowance_responsibility_abc_Year_Month_EmployeeCode");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_DepartmentName",
                table: "payroll_allowance_responsibility_abc",
                newName: "IX_payroll_allowance_responsibility_abc_Year_Month_DepartmentName");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_PositionId",
                table: "payroll_allowance_responsibility_abc",
                newName: "IX_payroll_allowance_responsibility_abc_PositionId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_GradeId",
                table: "payroll_allowance_responsibility_abc",
                newName: "IX_payroll_allowance_responsibility_abc_GradeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_monthly_responsibility_allowance_abc_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                newName: "IX_payroll_allowance_responsibility_abc_EmployeeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_allowance_responsibility_grade",
                table: "payroll_allowance_responsibility_grade",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_allowance_responsibility_grade_positions",
                table: "payroll_allowance_responsibility_grade_positions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_allowance_responsibility_employee_assignments",
                table: "payroll_allowance_responsibility_employee_assignments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_allowance_responsibility_abc",
                table: "payroll_allowance_responsibility_abc",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_grade_DisplayOrder",
                table: "payroll_allowance_responsibility_grade",
                sql: "\"DisplayOrder\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_grade_Month",
                table: "payroll_allowance_responsibility_grade",
                sql: "\"Month\" BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_grade_StandardResponsibili~",
                table: "payroll_allowance_responsibility_grade",
                sql: "\"StandardResponsibilityAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_grade_positions_Month",
                table: "payroll_allowance_responsibility_grade_positions",
                sql: "\"Month\" BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_employee_assignments_Month",
                table: "payroll_allowance_responsibility_employee_assignments",
                sql: "\"Month\" BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_employee_assignments_Stand~",
                table: "payroll_allowance_responsibility_employee_assignments",
                sql: "\"StandardResponsibilityAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_ActualResponsibilityAl~",
                table: "payroll_allowance_responsibility_abc",
                sql: "\"ActualResponsibilityAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_ActualWorkDays",
                table: "payroll_allowance_responsibility_abc",
                sql: "\"ActualWorkDays\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_Month",
                table: "payroll_allowance_responsibility_abc",
                sql: "\"Month\" BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_MonthlyPerformanceBonu~",
                table: "payroll_allowance_responsibility_abc",
                sql: "\"MonthlyPerformanceBonusAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_StandardResponsibility~",
                table: "payroll_allowance_responsibility_abc",
                sql: "\"StandardResponsibilityAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_StandardWorkDays",
                table: "payroll_allowance_responsibility_abc",
                sql: "\"StandardWorkDays\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_employees_EmployeeId",
                table: "payroll_allowance_responsibility_abc",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_payroll_allowance_resp~",
                table: "payroll_allowance_responsibility_abc",
                column: "GradeId",
                principalTable: "payroll_allowance_responsibility_grade",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_positions_PositionId",
                table: "payroll_allowance_responsibility_abc",
                column: "PositionId",
                principalTable: "positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_employee_assignments_emplo~",
                table: "payroll_allowance_responsibility_employee_assignments",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_employee_assignments_payro~",
                table: "payroll_allowance_responsibility_employee_assignments",
                column: "GradeId",
                principalTable: "payroll_allowance_responsibility_grade",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_grade_positions_payroll_al~",
                table: "payroll_allowance_responsibility_grade_positions",
                column: "GradeId",
                principalTable: "payroll_allowance_responsibility_grade",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_allowance_responsibility_grade_positions_positions_~",
                table: "payroll_allowance_responsibility_grade_positions",
                column: "PositionId",
                principalTable: "positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_employees_EmployeeId",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_payroll_allowance_resp~",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_abc_positions_PositionId",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_employee_assignments_emplo~",
                table: "payroll_allowance_responsibility_employee_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_employee_assignments_payro~",
                table: "payroll_allowance_responsibility_employee_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_grade_positions_payroll_al~",
                table: "payroll_allowance_responsibility_grade_positions");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_allowance_responsibility_grade_positions_positions_~",
                table: "payroll_allowance_responsibility_grade_positions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_allowance_responsibility_grade_positions",
                table: "payroll_allowance_responsibility_grade_positions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_grade_positions_Month",
                table: "payroll_allowance_responsibility_grade_positions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_allowance_responsibility_grade",
                table: "payroll_allowance_responsibility_grade");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_grade_DisplayOrder",
                table: "payroll_allowance_responsibility_grade");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_grade_Month",
                table: "payroll_allowance_responsibility_grade");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_grade_StandardResponsibili~",
                table: "payroll_allowance_responsibility_grade");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_allowance_responsibility_employee_assignments",
                table: "payroll_allowance_responsibility_employee_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_employee_assignments_Month",
                table: "payroll_allowance_responsibility_employee_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_employee_assignments_Stand~",
                table: "payroll_allowance_responsibility_employee_assignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_allowance_responsibility_abc",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_ActualResponsibilityAl~",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_ActualWorkDays",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_Month",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_MonthlyPerformanceBonu~",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_StandardResponsibility~",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_allowance_responsibility_abc_StandardWorkDays",
                table: "payroll_allowance_responsibility_abc");

            migrationBuilder.RenameTable(
                name: "payroll_allowance_responsibility_grade_positions",
                newName: "payroll_monthly_responsibility_allowance_grade_positions");

            migrationBuilder.RenameTable(
                name: "payroll_allowance_responsibility_grade",
                newName: "payroll_monthly_responsibility_allowance_grades");

            migrationBuilder.RenameTable(
                name: "payroll_allowance_responsibility_employee_assignments",
                newName: "payroll_monthly_responsibility_allowance_employee_assignments");

            migrationBuilder.RenameTable(
                name: "payroll_allowance_responsibility_abc",
                newName: "payroll_monthly_responsibility_allowance_abc");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_allowance_responsibility_grade_positions_Year_Month_PositionId",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                newName: "UX_payroll_monthly_responsibility_allowance_grade_positions_Year_Month_PositionId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_grade_positions_Year_Month_GradeId",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                newName: "IX_payroll_monthly_responsibility_allowance_grade_positions_Year_Month_GradeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_grade_positions_PositionId",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                newName: "IX_payroll_monthly_responsibility_allowance_grade_positions_Po~");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_grade_positions_GradeId",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                newName: "IX_payroll_monthly_responsibility_allowance_grade_positions_Gr~");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_allowance_responsibility_grade_Year_Month_Code",
                table: "payroll_monthly_responsibility_allowance_grades",
                newName: "UX_payroll_monthly_responsibility_allowance_grades_Year_Month_Code");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_grade_Year_Month_DisplayOrder_Code",
                table: "payroll_monthly_responsibility_allowance_grades",
                newName: "IX_payroll_monthly_responsibility_allowance_grades_Year_Month_DisplayOrder_Code");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_allowance_responsibility_employee_assignments_Year_Month_EmployeeId",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                newName: "UX_payroll_monthly_responsibility_allowance_employee_assignments_Year_Month_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_employee_assignments_Year_Month_GradeId",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                newName: "IX_payroll_monthly_responsibility_allowance_employee_assignments_Year_Month_GradeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_employee_assignments_Grade~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                newName: "IX_payroll_monthly_responsibility_allowance_employee_assignme~1");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_employee_assignments_Emplo~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                newName: "IX_payroll_monthly_responsibility_allowance_employee_assignmen~");

            migrationBuilder.RenameIndex(
                name: "UX_payroll_allowance_responsibility_abc_Year_Month_EmployeeId",
                table: "payroll_monthly_responsibility_allowance_abc",
                newName: "UX_payroll_monthly_responsibility_allowance_abc_Year_Month_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_abc_Year_Month_IsLocked",
                table: "payroll_monthly_responsibility_allowance_abc",
                newName: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_IsLocked");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_abc_Year_Month_EmployeeCode",
                table: "payroll_monthly_responsibility_allowance_abc",
                newName: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_EmployeeCode");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_abc_Year_Month_DepartmentName",
                table: "payroll_monthly_responsibility_allowance_abc",
                newName: "IX_payroll_monthly_responsibility_allowance_abc_Year_Month_DepartmentName");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_abc_PositionId",
                table: "payroll_monthly_responsibility_allowance_abc",
                newName: "IX_payroll_monthly_responsibility_allowance_abc_PositionId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_abc_GradeId",
                table: "payroll_monthly_responsibility_allowance_abc",
                newName: "IX_payroll_monthly_responsibility_allowance_abc_GradeId");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_allowance_responsibility_abc_EmployeeId",
                table: "payroll_monthly_responsibility_allowance_abc",
                newName: "IX_payroll_monthly_responsibility_allowance_abc_EmployeeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_monthly_responsibility_allowance_grade_positions",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_monthly_responsibility_allowance_grades",
                table: "payroll_monthly_responsibility_allowance_grades",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_monthly_responsibility_allowance_employee_assignmen~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_monthly_responsibility_allowance_abc",
                table: "payroll_monthly_responsibility_allowance_abc",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_grade_positions_Mo~",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                sql: "\"Month\" BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_grades_DisplayOrder",
                table: "payroll_monthly_responsibility_allowance_grades",
                sql: "\"DisplayOrder\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_grades_Month",
                table: "payroll_monthly_responsibility_allowance_grades",
                sql: "\"Month\" BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_grades_StandardRes~",
                table: "payroll_monthly_responsibility_allowance_grades",
                sql: "\"StandardResponsibilityAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_employee_assignme~1",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                sql: "\"StandardResponsibilityAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_employee_assignmen~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                sql: "\"Month\" BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_ActualResponsi~",
                table: "payroll_monthly_responsibility_allowance_abc",
                sql: "\"ActualResponsibilityAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_ActualWorkDays",
                table: "payroll_monthly_responsibility_allowance_abc",
                sql: "\"ActualWorkDays\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_Month",
                table: "payroll_monthly_responsibility_allowance_abc",
                sql: "\"Month\" BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_MonthlyPerform~",
                table: "payroll_monthly_responsibility_allowance_abc",
                sql: "\"MonthlyPerformanceBonusAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_StandardRespon~",
                table: "payroll_monthly_responsibility_allowance_abc",
                sql: "\"StandardResponsibilityAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_monthly_responsibility_allowance_abc_StandardWorkDa~",
                table: "payroll_monthly_responsibility_allowance_abc",
                sql: "\"StandardWorkDays\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_abc_employees_Empl~",
                table: "payroll_monthly_responsibility_allowance_abc",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_abc_payroll_monthl~",
                table: "payroll_monthly_responsibility_allowance_abc",
                column: "GradeId",
                principalTable: "payroll_monthly_responsibility_allowance_grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_abc_positions_Posi~",
                table: "payroll_monthly_responsibility_allowance_abc",
                column: "PositionId",
                principalTable: "positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_employee_assignmen~",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_employee_assignme~1",
                table: "payroll_monthly_responsibility_allowance_employee_assignments",
                column: "GradeId",
                principalTable: "payroll_monthly_responsibility_allowance_grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_grade_positions_pa~",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                column: "GradeId",
                principalTable: "payroll_monthly_responsibility_allowance_grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_monthly_responsibility_allowance_grade_positions_po~",
                table: "payroll_monthly_responsibility_allowance_grade_positions",
                column: "PositionId",
                principalTable: "positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
