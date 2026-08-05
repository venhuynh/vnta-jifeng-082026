using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

[Migration("20260727120000_RenameEmployeeTaxDependentsTable")]
public sealed class RenameEmployeeTaxDependentsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF to_regclass('public.payroll_decuction_employee_tax_dependents') IS NOT NULL
                   AND to_regclass('public.payroll_employee_tax_dependents') IS NULL THEN
                    ALTER TABLE public.payroll_decuction_employee_tax_dependents
                        RENAME TO payroll_employee_tax_dependents;
                END IF;
            END $$;

            ALTER INDEX IF EXISTS public."IX_payroll_decuction_employee_tax_dependents_EmployeeId_v2"
                RENAME TO "IX_payroll_employee_tax_dependents_EmployeeId_v2";
            ALTER INDEX IF EXISTS public."IX_payroll_decuction_employee_tax_dependents_EmployeeId_Registered_v2"
                RENAME TO "IX_payroll_employee_tax_dependents_EmployeeId_Registered_v2";
            ALTER INDEX IF EXISTS public."IX_payroll_decuction_employee_tax_dependents_EmployeeTaxCode_v2"
                RENAME TO "IX_payroll_employee_tax_dependents_EmployeeTaxCode_v2";
            ALTER INDEX IF EXISTS public."IX_payroll_decuction_employee_tax_dependents_DependentTaxCode_v2"
                RENAME TO "IX_payroll_employee_tax_dependents_DependentTaxCode_v2";
            ALTER INDEX IF EXISTS public."IX_payroll_decuction_employee_tax_dependents_DependentIdentityNumber_v2"
                RENAME TO "IX_payroll_employee_tax_dependents_DependentIdentityNumber_v2";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF to_regclass('public.payroll_employee_tax_dependents') IS NOT NULL
                   AND to_regclass('public.payroll_decuction_employee_tax_dependents') IS NULL THEN
                    ALTER TABLE public.payroll_employee_tax_dependents
                        RENAME TO payroll_decuction_employee_tax_dependents;
                END IF;
            END $$;

            ALTER INDEX IF EXISTS public."IX_payroll_employee_tax_dependents_EmployeeId_v2"
                RENAME TO "IX_payroll_decuction_employee_tax_dependents_EmployeeId_v2";
            ALTER INDEX IF EXISTS public."IX_payroll_employee_tax_dependents_EmployeeId_Registered_v2"
                RENAME TO "IX_payroll_decuction_employee_tax_dependents_EmployeeId_Registered_v2";
            ALTER INDEX IF EXISTS public."IX_payroll_employee_tax_dependents_EmployeeTaxCode_v2"
                RENAME TO "IX_payroll_decuction_employee_tax_dependents_EmployeeTaxCode_v2";
            ALTER INDEX IF EXISTS public."IX_payroll_employee_tax_dependents_DependentTaxCode_v2"
                RENAME TO "IX_payroll_decuction_employee_tax_dependents_DependentTaxCode_v2";
            ALTER INDEX IF EXISTS public."IX_payroll_employee_tax_dependents_DependentIdentityNumber_v2"
                RENAME TO "IX_payroll_decuction_employee_tax_dependents_DependentIdentityNumber_v2";
            """);
    }
}
