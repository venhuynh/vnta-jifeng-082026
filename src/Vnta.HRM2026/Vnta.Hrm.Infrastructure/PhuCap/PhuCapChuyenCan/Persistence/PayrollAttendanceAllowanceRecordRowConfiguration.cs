using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Persistence;

/// <summary>Preserves the established PostgreSQL table, names, constraints and concurrency token.</summary>
public sealed class PayrollAttendanceAllowanceRecordRowConfiguration : IEntityTypeConfiguration<PayrollAttendanceAllowanceRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollAttendanceAllowanceRecordRow> builder)
    {
        builder.ToTable("payroll_allowance_attendance_records", table =>
        {
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_StandardAllowanceAmount", "\"StandardAllowanceAmount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_StandardWorkdayCount", "\"StandardWorkdayCount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_ActualWorkdayCount", "TRUE");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_AdministrativeWorkdayCount", "\"AdministrativeWorkdayCount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_LateEarlyDeductionDays", "\"LateEarlyDeductionDays\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_AttendanceRate", "\"AttendanceRate\" >= 0 AND \"AttendanceRate\" <= 1");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_AllowanceAmount", "\"AllowanceAmount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_CtlWorkdayCount", "TRUE");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_LateEarlyMinutes", "\"LateEarlyMinutes\" IS NULL OR \"LateEarlyMinutes\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_attendance_records_Kqcc", "TRUE");
        });
        builder.HasKey(x => x.PayrollAllowanceSummaryRecordId);
        builder.Property(x => x.PayrollAllowanceSummaryRecordId).ValueGeneratedNever();
        builder.Property(x => x.StandardAllowanceAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.StandardWorkdayCount).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.ActualWorkdayCount).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.AdministrativeWorkdayCount).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.LateEarlyDeductionDays).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.AttendanceRate).HasPrecision(7, 4).IsRequired();
        builder.Property(x => x.AllowanceAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.AppliedRuleKey).HasMaxLength(32);
        builder.Property(x => x.AttendanceClass).HasMaxLength(16);
        builder.Property(x => x.CtlWorkdayCount).HasPrecision(10, 4);
        builder.Property(x => x.Kqcc).HasPrecision(10, 4);
        builder.Property(x => x.HasKpViolation).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Note).HasColumnType("text");
        builder.Property(x => x.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RefreshedAtUtc).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RefreshedBy).HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone").IsConcurrencyToken();
        builder.Property(x => x.UpdatedBy).HasMaxLength(128);
        builder.HasIndex(x => x.IsLocked).HasDatabaseName("IX_payroll_allowance_attendance_records_IsLocked");
        builder.HasIndex(x => x.AppliedRuleKey).HasDatabaseName("IX_payroll_allowance_attendance_records_AppliedRuleKey");
        builder.HasIndex(x => x.AttendanceClass).HasDatabaseName("IX_payroll_allowance_attendance_records_AttendanceClass");
        builder.HasOne<PayrollAllowanceSummaryRecordRow>().WithOne().HasForeignKey<PayrollAttendanceAllowanceRecordRow>(x => x.PayrollAllowanceSummaryRecordId).OnDelete(DeleteBehavior.Restrict);
    }
}
