using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePayrollFamilyDeductionSnapshotsAndRenameTaxDependents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS public.payroll_monthly_employee_pit_family_deductions;

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_decuction_employee_tax_dependents_employees_Employe~",
                table: "payroll_decuction_employee_tax_dependents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payroll_decuction_employee_tax_dependents",
                table: "payroll_decuction_employee_tax_dependents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payroll_decuction_employee_tax_dependents_DeductionRange",
                table: "payroll_decuction_employee_tax_dependents");

            migrationBuilder.RenameTable(
                name: "payroll_decuction_employee_tax_dependents",
                newName: "payroll_employee_tax_dependents");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_decuction_employee_tax_dependents_EmployeeTaxCode_v2",
                table: "payroll_employee_tax_dependents",
                newName: "IX_payroll_employee_tax_dependents_EmployeeTaxCode_v2");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_decuction_employee_tax_dependents_EmployeeId_v2",
                table: "payroll_employee_tax_dependents",
                newName: "IX_payroll_employee_tax_dependents_EmployeeId_v2");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_decuction_employee_tax_dependents_EmployeeId_Registered_v2",
                table: "payroll_employee_tax_dependents",
                newName: "IX_payroll_employee_tax_dependents_EmployeeId_Registered_v2");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_decuction_employee_tax_dependents_DependentTaxCode_v2",
                table: "payroll_employee_tax_dependents",
                newName: "IX_payroll_employee_tax_dependents_DependentTaxCode_v2");

            migrationBuilder.RenameIndex(
                name: "IX_payroll_decuction_employee_tax_dependents_DependentIdentityNumber_v2",
                table: "payroll_employee_tax_dependents",
                newName: "IX_payroll_employee_tax_dependents_DependentIdentityNumber_v2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payroll_employee_tax_dependents",
                table: "payroll_employee_tax_dependents",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "payroll_monthly_employee_pit_family_deductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationSnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DependentDeductionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    DependentDeductionCount = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    DependentSnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GhiChu = table.Column<string>(type: "text", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsManualOverride = table.Column<bool>(type: "boolean", nullable: false),
                    IsSelfDeductionApplied = table.Column<bool>(type: "boolean", nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Nam = table.Column<short>(type: "smallint", nullable: false),
                    PayrollDeductionSummaryRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyEffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    PolicyEffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    SelfDeductionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    TaxPolicyCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TaxResidenceStatus = table.Column<short>(type: "smallint", nullable: false),
                    Thang = table.Column<short>(type: "smallint", nullable: false),
                    TotalDependentDeductionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    TotalFamilyDeductionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_monthly_employee_pit_family_deductions", x => x.Id);
                    table.CheckConstraint("CK_payroll_monthly_employee_pit_family_deductions_Amounts", "\"SelfDeductionAmount\" >= 0 AND \"DependentDeductionCount\" >= 0 AND \"DependentDeductionAmount\" >= 0 AND \"TotalDependentDeductionAmount\" >= 0 AND \"TotalFamilyDeductionAmount\" >= 0");
                    table.CheckConstraint("CK_payroll_monthly_employee_pit_family_deductions_DependentTot~", "\"TotalDependentDeductionAmount\" = \"DependentDeductionCount\" * \"DependentDeductionAmount\"");
                    table.CheckConstraint("CK_payroll_monthly_employee_pit_family_deductions_FamilyTotal", "\"TotalFamilyDeductionAmount\" = \"SelfDeductionAmount\" + \"TotalDependentDeductionAmount\"");
                    table.CheckConstraint("CK_payroll_monthly_employee_pit_family_deductions_Nam", "\"Nam\" BETWEEN 1900 AND 2100");
                    table.CheckConstraint("CK_payroll_monthly_employee_pit_family_deductions_PolicyRange", "\"PolicyEffectiveTo\" IS NULL OR \"PolicyEffectiveTo\" >= \"PolicyEffectiveFrom\"");
                    table.CheckConstraint("CK_payroll_monthly_employee_pit_family_deductions_SelfApplied", "\"IsSelfDeductionApplied\" OR \"SelfDeductionAmount\" = 0");
                    table.CheckConstraint("CK_payroll_monthly_employee_pit_family_deductions_Thang", "\"Thang\" BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_payroll_monthly_employee_pit_family_deductions_employees_Em~",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_monthly_employee_pit_family_deductions_payroll_decu~",
                        column: x => x.PayrollDeductionSummaryRecordId,
                        principalTable: "payroll_decuction_summary_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_payroll_employee_tax_dependents_DeductionRange",
                table: "payroll_employee_tax_dependents",
                sql: "\"DeductionToMonth\" IS NULL OR \"DeductionFromMonth\" IS NULL OR \"DeductionToMonth\" >= \"DeductionFromMonth\"");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_employee_pit_family_deductions_IsLocked",
                table: "payroll_monthly_employee_pit_family_deductions",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_monthly_employee_pit_family_deductions_Nam_Thang",
                table: "payroll_monthly_employee_pit_family_deductions",
                columns: new[] { "Nam", "Thang" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_monthly_employee_pit_family_deductions_EmployeeId_Nam_Thang",
                table: "payroll_monthly_employee_pit_family_deductions",
                columns: new[] { "EmployeeId", "Nam", "Thang" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_payroll_monthly_employee_pit_family_deductions_SummaryId",
                table: "payroll_monthly_employee_pit_family_deductions",
                column: "PayrollDeductionSummaryRecordId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_employee_tax_dependents_employees_EmployeeId",
                table: "payroll_employee_tax_dependents",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
