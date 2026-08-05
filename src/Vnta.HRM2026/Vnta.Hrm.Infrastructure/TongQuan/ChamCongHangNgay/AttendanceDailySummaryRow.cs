namespace Vnta.Hrm.Infrastructure.TongQuan.ChamCongHangNgay;

public sealed class AttendanceDailySummaryRow
{
    public Guid Id { get; set; }

    public Guid? EmployeeId { get; set; }

    public DateOnly WorkDate { get; set; }

    public int PunchCount { get; set; }

    public string PunchMomentsText { get; set; } = string.Empty;

    public DateTime? FirstPunchTime { get; set; }

    public DateTime? LastPunchTime { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
