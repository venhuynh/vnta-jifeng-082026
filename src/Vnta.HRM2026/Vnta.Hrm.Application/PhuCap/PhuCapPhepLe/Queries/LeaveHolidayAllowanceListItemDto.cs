namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

public sealed record LeaveHolidayAllowanceListItemDto(
    Guid PayrollAllowanceSummaryRecordId,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal DailyWageAmount,
    decimal LeaveDayCount,
    decimal HolidayDayCount,
    decimal LeaveHolidayAllowanceAmount,
    string? Note,
    bool IsLocked,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    DateTime? DetailUpdatedAtUtc);
