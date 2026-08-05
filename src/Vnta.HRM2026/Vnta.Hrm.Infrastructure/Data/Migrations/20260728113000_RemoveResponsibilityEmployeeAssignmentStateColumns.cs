using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Assignment chỉ biểu diễn việc gán bậc. Trạng thái không hưởng được biểu diễn
/// bằng không có dòng assignment; trạng thái loại THS thuộc snapshot ABC.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260728113000_RemoveResponsibilityEmployeeAssignmentStateColumns")]
public partial class RemoveResponsibilityEmployeeAssignmentStateColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Dòng không có bậc chính là dữ liệu "không hưởng" cũ; từ nay không lưu assignment đó.
        migrationBuilder.Sql(
            """
            DELETE FROM payroll_allowance_responsibility_employee_assignments
            WHERE "GradeId" IS NULL;
            """);

        migrationBuilder.DropColumn(
            name: "IsActive",
            table: "payroll_allowance_responsibility_employee_assignments");

        migrationBuilder.DropColumn(
            name: "IsPerformanceBonusExcluded",
            table: "payroll_allowance_responsibility_employee_assignments");

        migrationBuilder.AlterColumn<Guid>(
            name: "GradeId",
            table: "payroll_allowance_responsibility_employee_assignments",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "GradeId",
            table: "payroll_allowance_responsibility_employee_assignments",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "payroll_allowance_responsibility_employee_assignments",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsPerformanceBonusExcluded",
            table: "payroll_allowance_responsibility_employee_assignments",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }
}
