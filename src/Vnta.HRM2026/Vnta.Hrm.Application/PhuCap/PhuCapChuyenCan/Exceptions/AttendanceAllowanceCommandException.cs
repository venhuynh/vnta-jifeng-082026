namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Exceptions;

/// <summary>
/// Phân loại lỗi command để transport không gộp validation, không tìm thấy,
/// khóa dữ liệu và cạnh tranh cập nhật vào cùng một HTTP status.
/// </summary>
public sealed class AttendanceAllowanceCommandException(
    AttendanceAllowanceCommandFailure failure,
    string message)
    : InvalidOperationException(message)
{
    public AttendanceAllowanceCommandFailure Failure { get; } = failure;
}

public enum AttendanceAllowanceCommandFailure
{
    Validation,
    NotFound,
    Locked,
    Concurrency
}
