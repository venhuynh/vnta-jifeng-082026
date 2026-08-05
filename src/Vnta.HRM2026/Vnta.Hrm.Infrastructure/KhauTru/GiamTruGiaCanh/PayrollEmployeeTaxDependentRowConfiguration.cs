using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vnta.Hrm.Infrastructure.KhauTru.GiamTruGiaCanh;

public sealed class PayrollEmployeeTaxDependentRowConfiguration : IEntityTypeConfiguration<PayrollEmployeeTaxDependentRow>
{
    public void Configure(EntityTypeBuilder<PayrollEmployeeTaxDependentRow> builder)
    {
        builder.ToTable("payroll_employee_tax_dependents", table =>
            table.HasCheckConstraint("CK_payroll_employee_tax_dependents_DeductionRange", "\"DeductionToMonth\" IS NULL OR \"DeductionFromMonth\" IS NULL OR \"DeductionToMonth\" >= \"DeductionFromMonth\""));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
        builder.Property(x => x.EmployeeTaxCode).HasColumnType("text");
        builder.Property(x => x.RegistrationDate).HasColumnType("date");
        builder.Property(x => x.DependentFullName).HasColumnType("text").IsRequired();
        builder.Property(x => x.DependentGender).HasColumnType("text");
        builder.Property(x => x.DependentBirthDate).HasColumnType("date");
        builder.Property(x => x.DependentIdentityNumber).HasColumnType("text");
        builder.Property(x => x.DependentTaxCode).HasColumnType("text");
        builder.Property(x => x.DependentNationality).HasColumnType("text");
        builder.Property(x => x.EmployeeIdentityNumber).HasColumnType("text");
        builder.Property(x => x.RelationshipToEmployee).HasColumnType("text");
        builder.Property(x => x.IsFamilyDeductionRegistered).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.RegistrationBookNumber).HasColumnType("text");
        builder.Property(x => x.RegistrationPageNumber).HasColumnType("text");
        builder.Property(x => x.CountryName).HasColumnType("text");
        builder.Property(x => x.OldWardCode).HasColumnType("text");
        builder.Property(x => x.OldWardName).HasColumnType("text");
        builder.Property(x => x.OldDistrictCode).HasColumnType("text");
        builder.Property(x => x.OldDistrictName).HasColumnType("text");
        builder.Property(x => x.OldProvinceCode).HasColumnType("text");
        builder.Property(x => x.OldProvinceName).HasColumnType("text");
        builder.Property(x => x.NewWardCode).HasColumnType("text");
        builder.Property(x => x.NewWardName).HasColumnType("text");
        builder.Property(x => x.NewDistrictCode).HasColumnType("text");
        builder.Property(x => x.NewDistrictName).HasColumnType("text");
        builder.Property(x => x.NewProvinceCode).HasColumnType("text");
        builder.Property(x => x.NewProvinceName).HasColumnType("text");
        builder.Property(x => x.DeductionFromMonth).HasColumnType("date");
        builder.Property(x => x.DeductionToMonth).HasColumnType("date");
        builder.Property(x => x.GhiChu).HasColumnType("text");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnType("text");
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp with time zone").IsConcurrencyToken();
        builder.Property(x => x.UpdatedBy).HasColumnType("text");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_payroll_employee_tax_dependents_EmployeeId_v2");
        builder.HasIndex(x => new { x.EmployeeId, x.IsFamilyDeductionRegistered }).HasDatabaseName("IX_payroll_employee_tax_dependents_EmployeeId_Registered_v2");
        builder.HasIndex(x => x.EmployeeTaxCode).HasDatabaseName("IX_payroll_employee_tax_dependents_EmployeeTaxCode_v2");
        builder.HasIndex(x => x.DependentTaxCode).HasDatabaseName("IX_payroll_employee_tax_dependents_DependentTaxCode_v2");
        builder.HasIndex(x => x.DependentIdentityNumber).HasDatabaseName("IX_payroll_employee_tax_dependents_DependentIdentityNumber_v2");
        builder.HasOne<Vnta.Hrm.Infrastructure.NhanSu.NhanVien.AttendanceGatewayEmployeeRow>().WithMany()
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
