namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed class AttendanceWorkdaySummaryRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public DateOnly WorkDate { get; set; }

    public string DayType { get; set; } = string.Empty;

    public Guid? ShiftId { get; set; }

    public string? ScheduledStartAt { get; set; }

    public string? ScheduledEndAt { get; set; }

    public string? CheckInAt { get; set; }

    public string? CheckOutAt { get; set; }

    public int LateMinutes { get; set; }

    public int EarlyLeaveMinutes { get; set; }

    public DateTime ComputedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? Note { get; set; }

    public Guid? CodeKetQuaTinhCongId { get; set; }

    public bool IsLocked { get; set; }

    public int OvertimeMinutes { get; set; }

    public int OvertimeMinutes15 { get; set; }

    public int OvertimeMinutes20 { get; set; }

    public int OvertimeMinutes30 { get; set; }

    public string? CheckInForOT15 { get; set; }

    public bool IsRegisterForOT { get; set; }

    public bool RequireDocument { get; set; }
}
