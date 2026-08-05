namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>Yêu cầu cập nhật các khoản cho phép nhập tay và ghi chú của một snapshot chưa khóa.</summary>
/// <remarks>
/// <paramref name="MealAllowanceAmount"/> được giữ lại để tương thích contract hiện hữu;
/// server không dùng giá trị này vì phụ cấp cơm là projection từ chi tiết.
/// </remarks>
public sealed record UpdatePayrollAllowanceSummaryManualValuesRequest(
    Guid Id,
    decimal ResponsibilityAllowanceAmount,
    decimal ResponsibilityOtherAllowanceAmount,
    decimal SeniorityAllowanceAmount,
    decimal AttendanceAllowanceAmount,
    decimal MealAllowanceAmount,
    decimal HazardAllowanceAmount,
    decimal OtherAllowanceAmount,
    decimal LeaveHolidayAllowanceAmount,
    string? Note,
    bool IsLocked,
    DateTime? OriginalUpdatedAtUtc,
    string? Actor);
