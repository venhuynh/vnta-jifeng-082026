using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260708093000_AddPayrollDeductionSummaryRecords")]
public sealed class AddPayrollDeductionSummaryRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.payroll_decuction_summary_records (
                "Id" uuid NOT NULL,
                "EmployeeId" uuid NOT NULL,
                "PayrollMonth" smallint NOT NULL,
                "PayrollYear" smallint NOT NULL,
                "BhxhYtAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "CongDoanAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "ThueTncnAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "TamUngAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "KhacAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "Note" text NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "CreatedBy" character varying(128) NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                "UpdatedBy" character varying(128) NULL,
                CONSTRAINT "PK_payroll_decuction_summary_records" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_payroll_decuction_summary_records_PayrollMonth"
                    CHECK ("PayrollMonth" >= 1 AND "PayrollMonth" <= 12),
                CONSTRAINT "CK_payroll_decuction_summary_records_PayrollYear"
                    CHECK ("PayrollYear" >= 1 AND "PayrollYear" <= 9999),
                CONSTRAINT "CK_payroll_decuction_summary_records_BhxhYtAmount"
                    CHECK ("BhxhYtAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_records_CongDoanAmount"
                    CHECK ("CongDoanAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_records_ThueTncnAmount"
                    CHECK ("ThueTncnAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_records_TamUngAmount"
                    CHECK ("TamUngAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_records_KhacAmount"
                    CHECK ("KhacAmount" >= 0)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_decuction_summary_records_EmployeeId_PayrollYear_PayrollMonth"
            ON public.payroll_decuction_summary_records ("EmployeeId", "PayrollYear", "PayrollMonth");

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_records_PayrollYear_PayrollMonth"
            ON public.payroll_decuction_summary_records ("PayrollYear", "PayrollMonth");

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_records_IsLocked"
            ON public.payroll_decuction_summary_records ("IsLocked");

            DO $$
            BEGIN
                IF to_regclass('public.employees') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_payroll_decuction_summary_records_employees_EmployeeId'
                    )
                THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    ADD CONSTRAINT "FK_payroll_decuction_summary_records_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id")
                    ON DELETE RESTRICT;
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS public.payroll_decuction_summary_records;
            """);
    }
}
