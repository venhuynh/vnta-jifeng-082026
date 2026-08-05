using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePayrollAllowanceMealRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO public.payroll_allowance_summary_records (
                    "Id", "EmployeeId", "PayrollMonth", "PayrollYear",
                    "ResponsibilityAllowanceAmount", "SeniorityAllowanceAmount", "AttendanceAllowanceAmount",
                    "MealAllowanceAmount", "HazardAllowanceAmount", "OtherAllowanceAmount", "LeaveHolidayAllowanceAmount",
                    "IsLocked", "Note", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
                SELECT
                    meal."Id", meal."EmployeeId", meal."PayrollMonth", meal."PayrollYear",
                    0, 0, 0, meal."MealAllowanceAmount", 0, 0, 0,
                    meal."IsLocked", meal."Note", meal."CreatedAtUtc",
                    COALESCE(meal."CreatedBy", 'meal-allowance'), meal."UpdatedAtUtc", meal."UpdatedBy"
                FROM public.payroll_meal_allowance_records AS meal
                LEFT JOIN public.payroll_allowance_summary_records AS summary
                    ON summary."EmployeeId" = meal."EmployeeId"
                    AND summary."PayrollYear" = meal."PayrollYear"
                    AND summary."PayrollMonth" = meal."PayrollMonth"
                WHERE summary."Id" IS NULL;

                UPDATE public.payroll_allowance_summary_records AS summary
                SET
                    "MealAllowanceAmount" = meal."MealAllowanceAmount",
                    "UpdatedAtUtc" = COALESCE(meal."UpdatedAtUtc", summary."UpdatedAtUtc"),
                    "UpdatedBy" = COALESCE(meal."UpdatedBy", summary."UpdatedBy")
                FROM public.payroll_meal_allowance_records AS meal
                WHERE summary."EmployeeId" = meal."EmployeeId"
                  AND summary."PayrollYear" = meal."PayrollYear"
                  AND summary."PayrollMonth" = meal."PayrollMonth";
                """);

            migrationBuilder.CreateTable(
                name: "payroll_allowance_meal_records",
                columns: table => new
                {
                    PayrollAllowanceSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollMonth = table.Column<short>(type: "smallint", nullable: false),
                    PayrollYear = table.Column<short>(type: "smallint", nullable: false),
                    QualifiedMealDays = table.Column<int>(type: "integer", nullable: false),
                    Overtime1900Days = table.Column<int>(type: "integer", nullable: false),
                    MealAllowancePerQualifiedDay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MealAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "qualified-meal"),
                    RuleVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_allowance_meal_records", x => x.PayrollAllowanceSummaryRecordId);
                    table.CheckConstraint("CK_payroll_allowance_meal_records_PayrollMonth", "\"PayrollMonth\" >= 1 AND \"PayrollMonth\" <= 12");
                    table.CheckConstraint("CK_payroll_allowance_meal_records_QualifiedMealDays", "\"QualifiedMealDays\" >= 0");
                    table.CheckConstraint("CK_payroll_allowance_meal_records_Overtime1900Days", "\"Overtime1900Days\" >= 0");
                    table.CheckConstraint("CK_payroll_allowance_meal_records_MealAllowancePerQualifiedDay", "\"MealAllowancePerQualifiedDay\" >= 0");
                    table.CheckConstraint("CK_payroll_allowance_meal_records_MealAllowanceAmount", "\"MealAllowanceAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_payroll_allowance_meal_records_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_allowance_meal_records_payroll_allowance_summary_records_PayrollAllowanceSummaryRecordId",
                        column: x => x.PayrollAllowanceSummaryRecordId,
                        principalTable: "payroll_allowance_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO public.payroll_allowance_meal_records (
                    "PayrollAllowanceSummaryRecordId", "EmployeeId", "PayrollMonth", "PayrollYear",
                    "QualifiedMealDays", "Overtime1900Days", "MealAllowancePerQualifiedDay", "MealAllowanceAmount",
                    "RuleCode", "RuleVersion", "Note", "IsLocked", "CalculatedAtUtc",
                    "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
                SELECT
                    summary."Id", meal."EmployeeId", meal."PayrollMonth", meal."PayrollYear",
                    meal."QualifiedMealDays", meal."Overtime1900Days", meal."MealAllowancePerQualifiedDay", meal."MealAllowanceAmount",
                    meal."RuleCode", meal."RuleVersion", meal."Note", meal."IsLocked", meal."CalculatedAtUtc",
                    meal."CreatedAtUtc", meal."CreatedBy", meal."UpdatedAtUtc", meal."UpdatedBy"
                FROM public.payroll_meal_allowance_records AS meal
                INNER JOIN public.payroll_allowance_summary_records AS summary
                    ON summary."EmployeeId" = meal."EmployeeId"
                    AND summary."PayrollYear" = meal."PayrollYear"
                    AND summary."PayrollMonth" = meal."PayrollMonth";
                """);

            migrationBuilder.DropTable(name: "payroll_meal_allowance_records");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_meal_records_IsLocked",
                table: "payroll_allowance_meal_records",
                column: "IsLocked");
            migrationBuilder.CreateIndex(
                name: "IX_payroll_allowance_meal_records_PayrollYear_PayrollMonth",
                table: "payroll_allowance_meal_records",
                columns: new[] { "PayrollYear", "PayrollMonth" });
            migrationBuilder.CreateIndex(
                name: "UX_payroll_allowance_meal_records_EmployeeId_PayrollYear_PayrollMonth",
                table: "payroll_allowance_meal_records",
                columns: new[] { "EmployeeId", "PayrollYear", "PayrollMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_meal_allowance_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollMonth = table.Column<short>(type: "smallint", nullable: false),
                    PayrollYear = table.Column<short>(type: "smallint", nullable: false),
                    QualifiedMealDays = table.Column<int>(type: "integer", nullable: false),
                    Overtime1900Days = table.Column<int>(type: "integer", nullable: false),
                    MealAllowancePerQualifiedDay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MealAllowanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "qualified-meal"),
                    RuleVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_meal_allowance_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_meal_allowance_records_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO public.payroll_meal_allowance_records (
                    "Id", "EmployeeId", "PayrollMonth", "PayrollYear", "QualifiedMealDays", "Overtime1900Days",
                    "MealAllowancePerQualifiedDay", "MealAllowanceAmount", "RuleCode", "RuleVersion", "Note", "IsLocked",
                    "CalculatedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
                SELECT
                    "PayrollAllowanceSummaryRecordId", "EmployeeId", "PayrollMonth", "PayrollYear", "QualifiedMealDays", "Overtime1900Days",
                    "MealAllowancePerQualifiedDay", "MealAllowanceAmount", "RuleCode", "RuleVersion", "Note", "IsLocked",
                    "CalculatedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy"
                FROM public.payroll_allowance_meal_records;
                """);

            migrationBuilder.DropTable(name: "payroll_allowance_meal_records");
        }
    }
}
