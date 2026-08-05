using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPayrollMealAllowanceRecordsToSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_meal_allowance_records_ActualWorkdayCount",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_meal_allowance_records_AttendanceRate",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_meal_allowance_records_StandardWorkdayCount",
                table: "payroll_meal_allowance_records");

            migrationBuilder.RenameColumn(
                name: "StandardAllowanceAmount",
                table: "payroll_meal_allowance_records",
                newName: "MealAllowancePerQualifiedDay");

            migrationBuilder.RenameColumn(
                name: "ActualAllowanceAmount",
                table: "payroll_meal_allowance_records",
                newName: "MealAllowanceAmount");

            migrationBuilder.AddColumn<DateTime>(
                name: "CalculatedAtUtc",
                table: "payroll_meal_allowance_records",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "payroll_meal_allowance_records",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "payroll_meal_allowance_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Overtime1900Days",
                table: "payroll_meal_allowance_records",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualifiedMealDays",
                table: "payroll_meal_allowance_records",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RuleCode",
                table: "payroll_meal_allowance_records",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "qualified-meal");

            migrationBuilder.AddColumn<string>(
                name: "RuleVersion",
                table: "payroll_meal_allowance_records",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "payroll_meal_allowance_records",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE public.payroll_meal_allowance_records
                SET
                    "QualifiedMealDays" = GREATEST(0, CEIL("ActualWorkdayCount"))::integer,
                    "Overtime1900Days" = GREATEST(0, CEIL("ActualWorkdayCount"))::integer,
                    "CalculatedAtUtc" = COALESCE("UpdatedAtUtc", "CreatedAtUtc", NOW()),
                    "RuleCode" = 'migrated-from-legacy',
                    "RuleVersion" = '2026-07-meal-legacy-migration',
                    "CreatedBy" = COALESCE("CreatedBy", 'meal-allowance'),
                    "UpdatedBy" = COALESCE("UpdatedBy", 'meal-allowance')
                WHERE "QualifiedMealDays" = 0
                  AND "Overtime1900Days" = 0;
                """);

            migrationBuilder.DropColumn(
                name: "ActualWorkdayCount",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "AttendanceRate",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "StandardWorkdayCount",
                table: "payroll_meal_allowance_records");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_meal_allowance_records_MealAllowanceAmount",
                table: "payroll_meal_allowance_records",
                sql: "\"MealAllowanceAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_meal_allowance_records_MealAllowancePerQualifiedDay",
                table: "payroll_meal_allowance_records",
                sql: "\"MealAllowancePerQualifiedDay\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_meal_allowance_records_Overtime1900Days",
                table: "payroll_meal_allowance_records",
                sql: "\"Overtime1900Days\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_meal_allowance_records_QualifiedMealDays",
                table: "payroll_meal_allowance_records",
                sql: "\"QualifiedMealDays\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_meal_allowance_records_MealAllowanceAmount",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_meal_allowance_records_MealAllowancePerQualifiedDay",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_meal_allowance_records_Overtime1900Days",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_meal_allowance_records_QualifiedMealDays",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "CalculatedAtUtc",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "Overtime1900Days",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "QualifiedMealDays",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "RuleCode",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "RuleVersion",
                table: "payroll_meal_allowance_records");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "payroll_meal_allowance_records");

            migrationBuilder.RenameColumn(
                name: "MealAllowancePerQualifiedDay",
                table: "payroll_meal_allowance_records",
                newName: "StandardAllowanceAmount");

            migrationBuilder.RenameColumn(
                name: "MealAllowanceAmount",
                table: "payroll_meal_allowance_records",
                newName: "ActualAllowanceAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWorkdayCount",
                table: "payroll_meal_allowance_records",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AttendanceRate",
                table: "payroll_meal_allowance_records",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardWorkdayCount",
                table: "payroll_meal_allowance_records",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_meal_allowance_records_ActualWorkdayCount",
                table: "payroll_meal_allowance_records",
                sql: "\"ActualWorkdayCount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_meal_allowance_records_AttendanceRate",
                table: "payroll_meal_allowance_records",
                sql: "\"AttendanceRate\" >= 0 AND \"AttendanceRate\" <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_meal_allowance_records_StandardWorkdayCount",
                table: "payroll_meal_allowance_records",
                sql: "\"StandardWorkdayCount\" > 0");
        }
    }
}
