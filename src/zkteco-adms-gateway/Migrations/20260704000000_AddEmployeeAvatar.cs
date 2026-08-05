using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.AttendanceGateway.Data;

#nullable disable

namespace Vnta.AttendanceGateway.Migrations
{
    [DbContext(typeof(ZktecoDbContext))]
    [Migration("20260704000000_AddEmployeeAvatar")]
    public partial class AddEmployeeAvatar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "employees",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "employees");
        }
    }
}
