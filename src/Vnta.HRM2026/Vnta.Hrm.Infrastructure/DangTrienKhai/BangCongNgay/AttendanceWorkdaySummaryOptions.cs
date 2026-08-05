namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed class AttendanceWorkdaySummaryOptions
{
    public const string SectionName = "AttendanceWorkdaySummary";

    // While the daily overtime registration source is not implemented yet,
    // regular-day overtime calculation bypasses registration validation.
    public bool EnableDailyOvertimeRegistrationCheck { get; set; }
}
