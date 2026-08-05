using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;

/// <summary>EF Core mapping owned by the leave/holiday allowance detail snapshot.</summary>
public sealed class PayrollAllowanceSummaryLeaveHolidayRecordRowConfiguration
    : IEntityTypeConfiguration<PayrollAllowanceSummaryLeaveHolidayRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollAllowanceSummaryLeaveHolidayRecordRow> builder)
    {
        builder.ToTable("payroll_allowance_summary_leave_holiday_records", table =>
        {
            table.HasCheckConstraint("CK_payroll_allowance_summary_leave_holiday_records_DailyWageAmount", "\"DailyWageAmount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_summary_leave_holiday_records_LeaveDayCount", "\"LeaveDayCount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_summary_leave_holiday_records_HolidayDayCount", "\"HolidayDayCount\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_summary_leave_holiday_records_LeaveHolidayAllowanceAmount", "\"LeaveHolidayAllowanceAmount\" >= 0");
        });

        builder.HasKey(x => x.PayrollAllowanceSummaryRecordId);
        builder.Property(x => x.PayrollAllowanceSummaryRecordId).ValueGeneratedNever();
        builder.Property(x => x.DailyWageAmount).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.LeaveDayCount).HasPrecision(9, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.HolidayDayCount).HasPrecision(9, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.LeaveHolidayAllowanceAmount).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.Note).HasColumnType("text");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedBy).HasMaxLength(128);
        builder.HasOne<PayrollAllowanceSummaryRecordRow>()
            .WithOne()
            .HasForeignKey<PayrollAllowanceSummaryLeaveHolidayRecordRow>(x => x.PayrollAllowanceSummaryRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
