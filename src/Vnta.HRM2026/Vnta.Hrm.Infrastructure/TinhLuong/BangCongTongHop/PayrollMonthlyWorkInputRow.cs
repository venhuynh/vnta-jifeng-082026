namespace Vnta.Hrm.Infrastructure.TinhLuong.BangCongTongHop;

/// <summary>
/// Snapshot công tổng hợp của một nhân viên trong một kỳ lương.
/// </summary>
public sealed class PayrollMonthlyWorkInputRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public short PayrollYear { get; set; }

    public short PayrollMonth { get; set; }

    public decimal AdministrativeWorkDays { get; set; }

    public int LateEarlyLeaveMinutes { get; set; }

    public int OvertimeMinutes15 { get; set; }

    public int OvertimeMinutes20 { get; set; }

    public int OvertimeMinutes30 { get; set; }

    public decimal PayrollWorkDays { get; set; }

    public bool IsLocked { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
