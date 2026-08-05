using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

public partial class RebuildPayrollEmployeeTaxDependentsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE EXTENSION IF NOT EXISTS pgcrypto;
            DROP TABLE IF EXISTS public.payroll_employee_tax_dependents;

            CREATE TABLE public.payroll_employee_tax_dependents
            (
                "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
                "EmployeeId" uuid NOT NULL,
                "EmployeeTaxCode" text NULL,
                "RegistrationDate" date NULL,
                "DependentFullName" text NOT NULL,
                "DependentGender" text NULL,
                "DependentBirthDate" date NULL,
                "DependentIdentityNumber" text NULL,
                "DependentTaxCode" text NULL,
                "DependentNationality" text NULL,
                "EmployeeIdentityNumber" text NULL,
                "RelationshipToEmployee" text NULL,
                "IsFamilyDeductionRegistered" boolean NOT NULL DEFAULT TRUE,
                "RegistrationBookNumber" text NULL,
                "RegistrationPageNumber" text NULL,
                "CountryName" text NULL,
                "OldWardCode" text NULL,
                "OldWardName" text NULL,
                "OldDistrictCode" text NULL,
                "OldDistrictName" text NULL,
                "OldProvinceCode" text NULL,
                "OldProvinceName" text NULL,
                "NewWardCode" text NULL,
                "NewWardName" text NULL,
                "NewDistrictCode" text NULL,
                "NewDistrictName" text NULL,
                "NewProvinceCode" text NULL,
                "NewProvinceName" text NULL,
                "DeductionFromMonth" date NULL,
                "DeductionToMonth" date NULL,
                "GhiChu" text NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT now(),
                "CreatedBy" text NULL,
                "UpdatedAtUtc" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                CONSTRAINT "PK_payroll_employee_tax_dependents_v2" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_payroll_employee_tax_dependents_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT,
                CONSTRAINT "CK_payroll_employee_tax_dependents_DeductionRange"
                    CHECK ("DeductionToMonth" IS NULL OR "DeductionFromMonth" IS NULL OR "DeductionToMonth" >= "DeductionFromMonth")
            );

            CREATE INDEX "IX_payroll_employee_tax_dependents_EmployeeId_v2"
                ON public.payroll_employee_tax_dependents ("EmployeeId");
            CREATE INDEX "IX_payroll_employee_tax_dependents_EmployeeId_Registered_v2"
                ON public.payroll_employee_tax_dependents ("EmployeeId", "IsFamilyDeductionRegistered");
            CREATE INDEX "IX_payroll_employee_tax_dependents_EmployeeTaxCode_v2"
                ON public.payroll_employee_tax_dependents ("EmployeeTaxCode");
            CREATE INDEX "IX_payroll_employee_tax_dependents_DependentTaxCode_v2"
                ON public.payroll_employee_tax_dependents ("DependentTaxCode");
            CREATE INDEX "IX_payroll_employee_tax_dependents_DependentIdentityNumber_v2"
                ON public.payroll_employee_tax_dependents ("DependentIdentityNumber");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS public.payroll_employee_tax_dependents;

            CREATE TABLE public.payroll_employee_tax_dependents
            (
                "Id" uuid NOT NULL PRIMARY KEY,
                "EmployeeId" uuid NOT NULL,
                "FullName" character varying(256) NOT NULL,
                "Gender" character varying(32) NULL,
                "DateOfBirth" date NULL,
                "IdentityDocumentNumber" character varying(64) NULL,
                "TaxCode" character varying(64) NULL,
                "Nationality" character varying(128) NULL,
                "Relationship" character varying(128) NULL,
                "IsRegisteredForFamilyDeduction" boolean NOT NULL DEFAULT FALSE,
                "RegistrationBookNumber" character varying(128) NULL,
                "RegistrationNumber" character varying(128) NULL,
                "AdministrativeAddress" text NULL,
                "DeductionFromMonth" date NULL,
                "DeductionToMonth" date NULL,
                "Note" text NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "CreatedBy" character varying(128) NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                "UpdatedBy" character varying(128) NULL,
                CONSTRAINT "FK_payroll_employee_tax_dependents_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT,
                CONSTRAINT "CK_payroll_employee_tax_dependents_DeductionRange"
                    CHECK ("DeductionToMonth" IS NULL OR "DeductionFromMonth" IS NULL OR "DeductionToMonth" >= "DeductionFromMonth")
            );

            CREATE INDEX "IX_payroll_employee_tax_dependents_EmployeeId"
                ON public.payroll_employee_tax_dependents ("EmployeeId");
            CREATE INDEX "IX_payroll_employee_tax_dependents_EmployeeId_Registered"
                ON public.payroll_employee_tax_dependents ("EmployeeId", "IsRegisteredForFamilyDeduction");
            """);
    }
}
