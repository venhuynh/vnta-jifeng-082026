namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>Yêu cầu cập nhật các khoản cho phép nhập tay và ghi chú của một snapshot chưa khóa.</summary>
/// <remarks>
/// <paramref name="AttendanceAllowanceAmount"/> là trường tương thích cho client cũ.
/// Phụ cấp chuyên cần là projection do màn hình Phụ cấp chuyên cần sở hữu; client mới phải gửi
/// <see langword="null"/>. Khi client cũ vẫn gửi trường này, server chỉ chấp nhận giá trị bằng
/// projection hiện có và tuyệt đối không dùng nó để ghi đè kết quả tính.
/// </remarks>
public sealed record UpdatePayrollAllowanceSummaryManualValuesRequest(
    Guid Id,
    decimal ResponsibilityAllowanceAmount,
    decimal ResponsibilityOtherAllowanceAmount,
    decimal SeniorityAllowanceAmount,
    decimal? AttendanceAllowanceAmount,
    decimal MealAllowanceAmount,
    decimal HazardAllowanceAmount,
    decimal OtherAllowanceAmount,
    decimal LeaveHolidayAllowanceAmount,
    string? Note,
    bool IsLocked,
    DateTime? OriginalUpdatedAtUtc,
    string? Actor);
