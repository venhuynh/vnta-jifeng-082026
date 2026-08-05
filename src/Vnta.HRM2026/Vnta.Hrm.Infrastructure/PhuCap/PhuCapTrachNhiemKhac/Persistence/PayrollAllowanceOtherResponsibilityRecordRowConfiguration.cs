using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac.Persistence;

public sealed class PayrollAllowanceOtherResponsibilityRecordRowConfiguration
    : IEntityTypeConfiguration<PayrollAllowanceOtherResponsibilityRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollAllowanceOtherResponsibilityRecordRow> builder)
    {
        builder.ToTable("payroll_allowance_other_responsibility_records", table =>
        {
            table.HasCheckConstraint("CK_payroll_allowance_other_responsibility_records_AllowanceWorkdayCount", "\"AllowanceWorkdayCount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_other_responsibility_records_StandardResponsibilityAllowanceAmount", "\"StandardResponsibilityAllowanceAmount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_other_responsibility_records_ActualResponsibilityAllowanceAmount", "\"ActualResponsibilityAllowanceAmount\" >= 0");
        });

        builder.HasKey(x => x.PayrollAllowanceSummaryRecordId);
        builder.Property(x => x.PayrollAllowanceSummaryRecordId).ValueGeneratedNever();
        builder.Property(x => x.AllowanceWorkdayCount).HasPrecision(10, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.StandardResponsibilityAllowanceAmount).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.ActualResponsibilityAllowanceAmount).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Note).HasColumnType("text");
        builder.Property(x => x.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RefreshedAtUtc).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RefreshedBy).HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone").IsConcurrencyToken();
        builder.Property(x => x.UpdatedBy).HasMaxLength(128);
        builder.HasIndex(x => x.IsLocked).HasDatabaseName("IX_payroll_allowance_other_responsibility_records_IsLocked");
        builder.HasOne<PayrollAllowanceSummaryRecordRow>()
            .WithOne()
            .HasForeignKey<PayrollAllowanceOtherResponsibilityRecordRow>(x => x.PayrollAllowanceSummaryRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
