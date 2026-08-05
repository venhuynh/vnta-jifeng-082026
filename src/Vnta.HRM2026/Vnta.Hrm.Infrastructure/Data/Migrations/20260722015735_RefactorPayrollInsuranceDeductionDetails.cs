using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

public partial class RefactorPayrollInsuranceDeductionDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.payroll_decuction_insurance_records
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_ActualAllowanceAmount",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_ActualWorkdayCount",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_AttendanceRate",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_StandardAllowanceAmount",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_StandardWorkdayCount";

            ALTER TABLE public.payroll_decuction_insurance_records
                RENAME COLUMN "StandardAllowanceAmount" TO "InsuranceSalaryBaseAmount";
            ALTER TABLE public.payroll_decuction_insurance_records
                RENAME COLUMN "ActualAllowanceAmount" TO "TotalDeductionAmount";

            ALTER TABLE public.payroll_decuction_insurance_records
                ADD COLUMN "SocialInsuranceRate" numeric(7,4) NOT NULL DEFAULT 0,
                ADD COLUMN "HealthInsuranceRate" numeric(7,4) NOT NULL DEFAULT 0,
                ADD COLUMN "UnemploymentInsuranceRate" numeric(7,4) NOT NULL DEFAULT 0.01,
                ADD COLUMN "TotalInsuranceRate" numeric(7,4) NOT NULL DEFAULT 0,
                ADD COLUMN "SocialInsuranceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                ADD COLUMN "HealthInsuranceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                ADD COLUMN "UnemploymentInsuranceAmount" numeric(18,2) NOT NULL DEFAULT 0,
                ADD COLUMN "IsParticipating" boolean NOT NULL DEFAULT TRUE,
                ADD COLUMN "ParticipationChangeType" smallint NOT NULL DEFAULT 0,
                ADD COLUMN "EffectiveDate" date NULL;

            UPDATE public.payroll_decuction_insurance_records
            SET "SocialInsuranceRate" = LEAST(1, GREATEST(0, CASE WHEN "StandardWorkdayCount" > 1 THEN "StandardWorkdayCount" / 100 ELSE "StandardWorkdayCount" END)),
                "HealthInsuranceRate" = LEAST(1, GREATEST(0, CASE WHEN "ActualWorkdayCount" > 1 THEN "ActualWorkdayCount" / 100 ELSE "ActualWorkdayCount" END));

            UPDATE public.payroll_decuction_insurance_records
            SET "TotalInsuranceRate" = ROUND("SocialInsuranceRate" + "HealthInsuranceRate" + "UnemploymentInsuranceRate", 4),
                "SocialInsuranceAmount" = ROUND("InsuranceSalaryBaseAmount" * "SocialInsuranceRate", 2),
                "HealthInsuranceAmount" = ROUND("InsuranceSalaryBaseAmount" * "HealthInsuranceRate", 2),
                "UnemploymentInsuranceAmount" = ROUND("InsuranceSalaryBaseAmount" * "UnemploymentInsuranceRate", 2);

            UPDATE public.payroll_decuction_insurance_records
            SET "TotalDeductionAmount" = "SocialInsuranceAmount" + "HealthInsuranceAmount" + "UnemploymentInsuranceAmount";

            UPDATE public.payroll_decuction_summary_records summary
            SET "SocialInsuranceDeductionAmount" = insurance."TotalDeductionAmount"
            FROM public.payroll_decuction_insurance_records insurance
            WHERE insurance."PayrollDeductionSummaryRecordId" = summary."Id";

            ALTER TABLE public.payroll_decuction_insurance_records
                DROP COLUMN "StandardWorkdayCount",
                DROP COLUMN "ActualWorkdayCount",
                DROP COLUMN "AttendanceRate";

            ALTER TABLE public.payroll_decuction_insurance_records
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_InsuranceSalaryBaseAmount" CHECK ("InsuranceSalaryBaseAmount" >= 0),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_SocialInsuranceRate" CHECK ("SocialInsuranceRate" BETWEEN 0 AND 1),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_HealthInsuranceRate" CHECK ("HealthInsuranceRate" BETWEEN 0 AND 1),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_UnemploymentInsuranceRate" CHECK ("UnemploymentInsuranceRate" BETWEEN 0 AND 1),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_TotalInsuranceRate" CHECK ("TotalInsuranceRate" BETWEEN 0 AND 1),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_TotalDeductionAmount" CHECK ("TotalDeductionAmount" >= 0),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_ParticipationChangeType" CHECK ("ParticipationChangeType" BETWEEN 0 AND 3);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.payroll_decuction_insurance_records
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_InsuranceSalaryBaseAmount",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_SocialInsuranceRate",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_HealthInsuranceRate",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_UnemploymentInsuranceRate",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_TotalInsuranceRate",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_TotalDeductionAmount",
                DROP CONSTRAINT IF EXISTS "CK_payroll_decuction_insurance_records_ParticipationChangeType";

            ALTER TABLE public.payroll_decuction_insurance_records
                ADD COLUMN "StandardWorkdayCount" numeric(10,2) NOT NULL DEFAULT 1,
                ADD COLUMN "ActualWorkdayCount" numeric(10,2) NOT NULL DEFAULT 0,
                ADD COLUMN "AttendanceRate" numeric(7,4) NOT NULL DEFAULT 0;
            UPDATE public.payroll_decuction_insurance_records
            SET "StandardWorkdayCount" = "SocialInsuranceRate",
                "ActualWorkdayCount" = "HealthInsuranceRate",
                "AttendanceRate" = "TotalInsuranceRate";
            ALTER TABLE public.payroll_decuction_insurance_records
                DROP COLUMN "SocialInsuranceRate", DROP COLUMN "HealthInsuranceRate", DROP COLUMN "UnemploymentInsuranceRate",
                DROP COLUMN "TotalInsuranceRate", DROP COLUMN "SocialInsuranceAmount", DROP COLUMN "HealthInsuranceAmount",
                DROP COLUMN "UnemploymentInsuranceAmount", DROP COLUMN "IsParticipating", DROP COLUMN "ParticipationChangeType", DROP COLUMN "EffectiveDate";
            ALTER TABLE public.payroll_decuction_insurance_records RENAME COLUMN "InsuranceSalaryBaseAmount" TO "StandardAllowanceAmount";
            ALTER TABLE public.payroll_decuction_insurance_records RENAME COLUMN "TotalDeductionAmount" TO "ActualAllowanceAmount";
            ALTER TABLE public.payroll_decuction_insurance_records
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_StandardAllowanceAmount" CHECK ("StandardAllowanceAmount" >= 0),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_StandardWorkdayCount" CHECK ("StandardWorkdayCount" > 0),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_ActualWorkdayCount" CHECK ("ActualWorkdayCount" >= 0),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_AttendanceRate" CHECK ("AttendanceRate" BETWEEN 0 AND 1),
                ADD CONSTRAINT "CK_payroll_decuction_insurance_records_ActualAllowanceAmount" CHECK ("ActualAllowanceAmount" >= 0);
            """);
    }
}
