namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;

public sealed class PayrollAllowanceSummaryLeaveHolidayRecordRow
{
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    public decimal DailyWageAmount { get; set; }

    public decimal LeaveDayCount { get; set; }

    public decimal HolidayDayCount { get; set; }

    public decimal LeaveHolidayAllowanceAmount { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
