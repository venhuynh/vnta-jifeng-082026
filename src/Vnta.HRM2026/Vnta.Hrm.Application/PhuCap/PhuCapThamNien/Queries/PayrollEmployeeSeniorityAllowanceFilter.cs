namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public sealed record PayrollEmployeeSeniorityAllowanceFilter(
    int PayrollMonth,
    int PayrollYear,
    string? DepartmentName = null,
    string? SearchText = null,
    bool? IsLocked = null,
    int Take = 2000,
    int Skip = 0,
    string? SeniorityRangeKey = null);
