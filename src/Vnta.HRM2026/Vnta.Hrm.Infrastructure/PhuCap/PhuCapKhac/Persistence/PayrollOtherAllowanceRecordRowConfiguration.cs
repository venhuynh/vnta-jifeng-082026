using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.Data.Configurations;

public sealed class PayrollOtherAllowanceRecordRowConfiguration : IEntityTypeConfiguration<PayrollOtherAllowanceRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollOtherAllowanceRecordRow> builder)
    {
        builder.ToTable("payroll_allowance_other", table =>
        {
            table.HasCheckConstraint("CK_payroll_allowance_other_AllowanceAmount", "\"AllowanceAmount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_other_NonFixedAmountIsZero", "\"IsFixedAmount\" OR \"AllowanceAmount\" = 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AllowanceName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IsFixedAmount).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.AllowanceAmount).HasPrecision(18, 0).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Note).HasColumnType("text");
        builder.Property(x => x.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone").IsConcurrencyToken();
        builder.Property(x => x.UpdatedBy).HasMaxLength(128);
        builder.HasIndex(x => x.PayrollAllowanceSummaryRecordId).HasDatabaseName("IX_payroll_allowance_other_PayrollAllowanceSummaryRecordId");
        builder.HasIndex(x => x.IsLocked).HasDatabaseName("IX_payroll_allowance_other_IsLocked");
        builder.HasOne<PayrollAllowanceSummaryRecordRow>().WithMany()
            .HasForeignKey(x => x.PayrollAllowanceSummaryRecordId).OnDelete(DeleteBehavior.Restrict);
    }
}
