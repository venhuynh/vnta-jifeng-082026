namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

/// <summary>
/// Thực thể persistence của một snapshot tổng hợp phụ cấp cho một nhân viên trong một kỳ lương.
/// Các khoản tiền là giá trị đã chốt tại thời điểm đồng bộ/làm mới, không phải phép tính động từ giao diện.
/// </summary>
public sealed class PayrollAllowanceSummaryRecordRow
{
    #region Định danh kỳ lương

    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public short PayrollMonth { get; set; }

    public short PayrollYear { get; set; }

    #endregion

    #region Các khoản phụ cấp đã tổng hợp

    public decimal ResponsibilityAllowanceAmount { get; set; }

    /// <summary>Phụ cấp trách nhiệm khác, lấy từ payroll_allowance_other_responsibility_records.</summary>
    public decimal ResponsibilityOtherAllowanceAmount { get; set; }

    public decimal SeniorityAllowanceAmount { get; set; }

    public decimal AttendanceAllowanceAmount { get; set; }

    public decimal MealAllowanceAmount { get; set; }

    public decimal HazardAllowanceAmount { get; set; }

    public decimal OtherAllowanceAmount { get; set; }

    public decimal LeaveHolidayAllowanceAmount { get; set; }

    #endregion

    #region Trạng thái, ghi chú và audit

    public bool IsLocked { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    #endregion
}
