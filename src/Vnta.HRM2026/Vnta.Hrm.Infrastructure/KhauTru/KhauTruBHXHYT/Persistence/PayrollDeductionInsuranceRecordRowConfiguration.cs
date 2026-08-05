using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;

/// <summary>Feature-owned mapping. Names are intentionally identical to the historical schema.</summary>
public sealed class PayrollDeductionInsuranceRecordRowConfiguration
    : IEntityTypeConfiguration<PayrollDeductionInsuranceRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollDeductionInsuranceRecordRow> builder)
    {
        builder.ToTable(
            "payroll_decuction_insurance_records",
            table =>
            {
                table.HasCheckConstraint("CK_payroll_decuction_insurance_records_InsuranceSalaryBaseAmount", "\"InsuranceSalaryBaseAmount\" >= 0");
                table.HasCheckConstraint("CK_payroll_decuction_insurance_records_SocialInsuranceRate", "\"SocialInsuranceRate\" >= 0 AND \"SocialInsuranceRate\" <= 1");
                table.HasCheckConstraint("CK_payroll_decuction_insurance_records_HealthInsuranceRate", "\"HealthInsuranceRate\" >= 0 AND \"HealthInsuranceRate\" <= 1");
                table.HasCheckConstraint("CK_payroll_decuction_insurance_records_UnemploymentInsuranceRate", "\"UnemploymentInsuranceRate\" >= 0 AND \"UnemploymentInsuranceRate\" <= 1");
                table.HasCheckConstraint("CK_payroll_decuction_insurance_records_TotalInsuranceRate", "\"TotalInsuranceRate\" >= 0 AND \"TotalInsuranceRate\" <= 1");
                table.HasCheckConstraint("CK_payroll_decuction_insurance_records_TotalDeductionAmount", "\"TotalDeductionAmount\" >= 0");
                table.HasCheckConstraint("CK_payroll_decuction_insurance_records_ParticipationChangeType", "\"ParticipationChangeType\" BETWEEN 0 AND 3");
            });

        builder.HasKey(x => x.PayrollDeductionSummaryRecordId);
        builder.Property(x => x.PayrollDeductionSummaryRecordId).ValueGeneratedNever();
        builder.Property(x => x.InsuranceSalaryBaseAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.SocialInsuranceRate).HasPrecision(7, 4).IsRequired();
        builder.Property(x => x.HealthInsuranceRate).HasPrecision(7, 4).IsRequired();
        builder.Property(x => x.UnemploymentInsuranceRate).HasPrecision(7, 4).IsRequired();
        builder.Property(x => x.TotalInsuranceRate).HasPrecision(7, 4).IsRequired();
        builder.Property(x => x.SocialInsuranceAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.HealthInsuranceAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.UnemploymentInsuranceAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TotalDeductionAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.IsParticipating).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.ParticipationChangeType).HasColumnType("smallint").HasDefaultValue((short)0).IsRequired();
        builder.Property(x => x.EffectiveDate).HasColumnType("date");
        builder.Property(x => x.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone");
        builder.HasIndex(x => x.IsLocked).HasDatabaseName("IX_payroll_decuction_insurance_records_IsLocked");
        builder.HasOne<PayrollDeductionSummaryRecordRow>()
            .WithOne()
            .HasForeignKey<PayrollDeductionInsuranceRecordRow>(x => x.PayrollDeductionSummaryRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
