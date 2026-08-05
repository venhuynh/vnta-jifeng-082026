using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceOvertimeRegistrationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance_overtime_registration_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DayType = table.Column<short>(type: "smallint", nullable: false),
                    WorkshopCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkshopName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    LastActionAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_overtime_registration_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_overtime_registration_requests_employees_Approve~",
                        column: x => x.ApprovedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_overtime_registration_requests_employees_Request~",
                        column: x => x.RequestedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_overtime_registration_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PositionName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TeamCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AssignmentType = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_overtime_registration_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_overtime_registration_details_attendance_overtim~",
                        column: x => x.RequestId,
                        principalTable: "attendance_overtime_registration_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendance_overtime_registration_details_employees_Employee~",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_overtime_registration_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<short>(type: "smallint", nullable: true),
                    ToStatus = table.Column<short>(type: "smallint", nullable: false),
                    ActionName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    PerformedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PerformedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_overtime_registration_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_overtime_registration_histories_attendance_overt~",
                        column: x => x.RequestId,
                        principalTable: "attendance_overtime_registration_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendance_overtime_registration_histories_employees_Perfor~",
                        column: x => x.PerformedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_details_EmployeeId",
                table: "attendance_overtime_registration_details",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_details_RequestId",
                table: "attendance_overtime_registration_details",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "UX_attendance_overtime_registration_details_RequestId_EmployeeId",
                table: "attendance_overtime_registration_details",
                columns: new[] { "RequestId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_histories_PerformedAtUtc",
                table: "attendance_overtime_registration_histories",
                column: "PerformedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_histories_PerformedByEmplo~",
                table: "attendance_overtime_registration_histories",
                column: "PerformedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_histories_RequestId",
                table: "attendance_overtime_registration_histories",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_requests_ApprovedByEmploye~",
                table: "attendance_overtime_registration_requests",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_requests_RequestedByEmploy~",
                table: "attendance_overtime_registration_requests",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_requests_Status",
                table: "attendance_overtime_registration_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_overtime_registration_requests_WorkDate",
                table: "attendance_overtime_registration_requests",
                column: "WorkDate");

            migrationBuilder.CreateIndex(
                name: "UX_attendance_overtime_registration_requests_WorkshopCode_WorkDate",
                table: "attendance_overtime_registration_requests",
                columns: new[] { "WorkshopCode", "WorkDate" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_overtime_registration_details");

            migrationBuilder.DropTable(
                name: "attendance_overtime_registration_histories");

            migrationBuilder.DropTable(
                name: "attendance_overtime_registration_requests");
        }
    }
}
