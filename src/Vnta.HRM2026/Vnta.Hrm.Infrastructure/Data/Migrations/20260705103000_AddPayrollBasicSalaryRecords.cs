using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260705103000_AddPayrollBasicSalaryRecords")]
public sealed class AddPayrollBasicSalaryRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.payroll_basic_salary_records
            (
                "Id" uuid NOT NULL,
                "EmployeeId" uuid NOT NULL,
                "PayrollMonth" integer NOT NULL,
                "PayrollYear" integer NOT NULL,
                "BasicSalary" numeric(18,2) NOT NULL,
                "StandardWorkingDays" numeric(5,2) NOT NULL,
                "DailySalary" numeric(18,4) NOT NULL,
                "HourlySalary" numeric(18,4) NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_payroll_basic_salary_records" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_payroll_basic_salary_records_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT,
                CONSTRAINT "CK_payroll_basic_salary_records_PayrollMonth"
                    CHECK ("PayrollMonth" BETWEEN 1 AND 12),
                CONSTRAINT "CK_payroll_basic_salary_records_PayrollYear"
                    CHECK ("PayrollYear" BETWEEN 1 AND 9999),
                CONSTRAINT "CK_payroll_basic_salary_records_BasicSalary"
                    CHECK ("BasicSalary" > 0),
                CONSTRAINT "CK_payroll_basic_salary_records_StandardWorkingDays"
                    CHECK ("StandardWorkingDays" > 0),
                CONSTRAINT "CK_payroll_basic_salary_records_DailySalary"
                    CHECK ("DailySalary" >= 0),
                CONSTRAINT "CK_payroll_basic_salary_records_HourlySalary"
                    CHECK ("HourlySalary" >= 0)
            );

            CREATE INDEX IF NOT EXISTS "IX_payroll_basic_salary_records_EmployeeId"
            ON public.payroll_basic_salary_records ("EmployeeId");

            CREATE INDEX IF NOT EXISTS "IX_payroll_basic_salary_records_PayrollYear_PayrollMonth"
            ON public.payroll_basic_salary_records ("PayrollYear", "PayrollMonth");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_payroll_basic_salary_records_EmployeeId_PayrollYear_PayrollMonth"
            ON public.payroll_basic_salary_records ("EmployeeId", "PayrollYear", "PayrollMonth");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS public.payroll_basic_salary_records;
            """);
    }
}
