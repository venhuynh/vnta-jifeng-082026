using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeePersonalInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_citizen_identities",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CitizenIdentityNumberCiphertext = table.Column<string>(type: "text", nullable: false),
                    CitizenIdentityNumberHash = table.Column<string>(type: "char(64)", nullable: false),
                    IssuedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IssuedPlace = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_citizen_identities", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_employee_citizen_identities_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_contact_profiles",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonalEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PersonalPhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PermanentAddress = table.Column<string>(type: "text", nullable: true),
                    CurrentAddress = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EmergencyContactRelationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_contact_profiles", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_employee_contact_profiles_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_employee_citizen_identities_NumberHash",
                table: "employee_citizen_identities",
                column: "CitizenIdentityNumberHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_citizen_identities");

            migrationBuilder.DropTable(
                name: "employee_contact_profiles");
        }
    }
}
