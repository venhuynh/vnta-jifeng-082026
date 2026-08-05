using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Persistence;

/// <summary>Maps the seniority feature aggregate without changing its established database contract.</summary>
public sealed class PayrollEmployeeSeniorityAllowanceRowConfiguration
    : IEntityTypeConfiguration<PayrollEmployeeSeniorityAllowanceRow>
{
    public void Configure(EntityTypeBuilder<PayrollEmployeeSeniorityAllowanceRow> builder)
    {
        builder.ToTable("payroll_allowance_seniority_records", table =>
        {
            table.HasCheckConstraint("CK_payroll_allowance_seniority_records_CompletedSeniorityYears", "\"CompletedSeniorityYears\" IS NULL OR \"CompletedSeniorityYears\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_seniority_records_CompletedSeniorityMonths", "\"CompletedSeniorityMonths\" IS NULL OR (\"CompletedSeniorityMonths\" >= 0 AND \"CompletedSeniorityMonths\" < 12)");
            table.HasCheckConstraint("CK_payroll_allowance_seniority_records_AdministrativeWorkDays", "\"AdministrativeWorkDays\" IS NULL OR \"AdministrativeWorkDays\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_seniority_records_LateEarlyLeaveWorkDays", "\"LateEarlyLeaveWorkDays\" IS NULL OR \"LateEarlyLeaveWorkDays\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_seniority_records_AllowanceAmount", "\"AllowanceAmount\" >= 0");
        });

        builder.HasKey(x => x.PayrollAllowanceSummaryRecordId);
        builder.Property(x => x.PayrollAllowanceSummaryRecordId).ValueGeneratedNever();
        builder.Property(x => x.AdministrativeWorkDays).HasPrecision(9, 4);
        builder.Property(x => x.LateEarlyLeaveWorkDays).HasPrecision(9, 4);
        builder.Property(x => x.SalaryWorkDays).HasPrecision(9, 4);
        builder.Property(x => x.AppliedRuleKey).HasMaxLength(32);
        builder.Property(x => x.AllowanceAmount).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Note).HasColumnType("text");
        builder.Property(x => x.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RefreshedAtUtc).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RefreshedBy).HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone").IsConcurrencyToken();
        builder.Property(x => x.UpdatedBy).HasMaxLength(128);
        builder.HasIndex(x => x.IsLocked).HasDatabaseName("IX_payroll_allowance_seniority_records_IsLocked");
        builder.HasIndex(x => x.AppliedRuleKey).HasDatabaseName("IX_payroll_allowance_seniority_records_AppliedRuleKey");
        builder.HasOne<PayrollAllowanceSummaryRecordRow>().WithOne()
            .HasForeignKey<PayrollEmployeeSeniorityAllowanceRow>(x => x.PayrollAllowanceSummaryRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
