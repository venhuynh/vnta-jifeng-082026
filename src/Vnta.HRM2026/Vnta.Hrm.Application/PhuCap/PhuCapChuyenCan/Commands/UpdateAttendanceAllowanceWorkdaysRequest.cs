namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

/// <summary>
/// Atomically replaces both manually editable workday values for one attendance-allowance aggregate.
/// <see cref="OriginalUpdatedAtUtc"/> is the aggregate's single optimistic-concurrency version.
/// </summary>
public sealed record UpdateAttendanceAllowanceWorkdaysRequest(
    Guid Id,
    decimal ActualWorkdayCount,
    decimal StandardWorkdayCount,
    DateTime? OriginalUpdatedAtUtc);
