using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Lưu ngày nghỉ việc và ngày bắt đầu tính thâm niên độc lập với ngày vào làm.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260718090000_AddEmployeeStatusDates")]
public partial class AddEmployeeStatusDates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ResignedDate",
            table: "employees",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SeniorityStartDate",
            table: "employees",
            type: "date",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ResignedDate",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "SeniorityStartDate",
            table: "employees");
    }
}
