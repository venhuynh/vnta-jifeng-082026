using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>Yêu cầu đọc các ngày công của một kỳ để tính phụ cấp chuyên cần.</summary>
public sealed record AttendanceAllowanceWorkdaySourceRequest(
    short PayrollYear,
    short PayrollMonth,
    IReadOnlyCollection<Guid> EmployeeIds);

/// <summary>
/// Boundary đọc cấu hình mã chấm công và dữ liệu bảng công. Adapter hạ tầng chịu
/// trách nhiệm hiện thực; policy không phụ thuộc EF hay nguồn dữ liệu bên ngoài.
/// </summary>
public interface IAttendanceAllowanceWorkdaySource
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<AttendanceAllowanceWorkdayInput>>> LoadByEmployeeIdAsync(
        AttendanceAllowanceWorkdaySourceRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> LoadEligibleStatusCodesAsync(CancellationToken cancellationToken = default);
}
