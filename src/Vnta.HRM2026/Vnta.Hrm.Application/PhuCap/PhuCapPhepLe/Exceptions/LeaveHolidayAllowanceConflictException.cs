namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Exceptions;

/// <summary>
/// Báo hiệu dữ liệu đã đổi sau thời điểm người dùng mở dòng để thao tác.
/// Endpoint ánh xạ ngoại lệ này thành HTTP 409 để UI yêu cầu tải lại snapshot.
/// </summary>
public sealed class LeaveHolidayAllowanceConflictException(string message)
    : InvalidOperationException(message);
