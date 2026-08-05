using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

/// <summary>Feature-owned mapping; table, columns, constraints and concurrency token retain their historical names.</summary>
public sealed class PayrollHazardAllowanceRecordRowConfiguration : IEntityTypeConfiguration<PayrollHazardAllowanceRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollHazardAllowanceRecordRow> builder)
    {
        builder.ToTable("payroll_allowance_hazard_records", table =>
        {
            table.HasCheckConstraint("CK_payroll_allowance_hazard_records_QualifiedWorkdayCount", "\"QualifiedWorkdayCount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_hazard_records_LateEarlyDeductionDays", "\"LateEarlyDeductionDays\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_hazard_records_PayableWorkdayCount", "\"PayableWorkdayCount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_hazard_records_HazardAllowancePerDay", "\"HazardAllowancePerDay\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_hazard_records_HazardAllowanceAmount", "\"HazardAllowanceAmount\" >= 0");
        });
        builder.HasKey(row => row.PayrollAllowanceSummaryRecordId);
        builder.Property(row => row.PayrollAllowanceSummaryRecordId).ValueGeneratedNever();
        builder.Property(row => row.QualifiedWorkdayCount).HasPrecision(10, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(row => row.LateEarlyDeductionDays).HasPrecision(10, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(row => row.PayableWorkdayCount).HasPrecision(10, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(row => row.HazardAllowancePerDay).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(row => row.HazardAllowanceAmount).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(row => row.IsEligibleDepartment).HasDefaultValue(false).IsRequired();
        builder.Property(row => row.IsEligibleForAllowance).HasDefaultValue(false).IsRequired();
        builder.Property(row => row.ExclusionReason).HasColumnType("text");
        builder.Property(row => row.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(row => row.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(row => row.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(row => row.UpdatedAtUtc).HasColumnType("timestamp without time zone").IsConcurrencyToken();
        builder.Property(row => row.UpdatedBy).HasMaxLength(128);
        builder.HasIndex(row => row.IsLocked).HasDatabaseName("IX_payroll_allowance_hazard_records_IsLocked");
        builder.HasOne<PayrollAllowanceSummaryRecordRow>().WithOne()
            .HasForeignKey<PayrollHazardAllowanceRecordRow>(row => row.PayrollAllowanceSummaryRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
