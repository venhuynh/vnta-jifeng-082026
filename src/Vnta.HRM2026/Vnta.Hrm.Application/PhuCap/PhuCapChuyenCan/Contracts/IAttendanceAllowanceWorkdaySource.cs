using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>Yêu cầu đọc các ngày công của một kỳ để tính phụ cấp chuyên cần.</summary>
public sealed record AttendanceAllowanceWorkdaySourceRequest(
    short PayrollYear,
    short PayrollMonth,
    IReadOnlyCollection<Guid> EmployeeIds);

/// <summary>Loads workday inputs required to calculate an attendance-allowance snapshot.</summary>
public interface IAttendanceAllowanceWorkdayInputSource
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<AttendanceAllowanceWorkdayInput>>> LoadByEmployeeIdAsync(
        AttendanceAllowanceWorkdaySourceRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Loads configured attendance-status codes eligible for attendance allowance.</summary>
public interface IAttendanceAllowanceEligibleStatusCodeSource
{
    Task<IReadOnlyList<string>> LoadEligibleStatusCodesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Backwards-compatible aggregate boundary for adapters that provide both workday inputs and eligible
/// status codes. New consumers should depend on the narrower capability they require.
/// </summary>
public interface IAttendanceAllowanceWorkdaySource :
    IAttendanceAllowanceWorkdayInputSource,
    IAttendanceAllowanceEligibleStatusCodeSource;
