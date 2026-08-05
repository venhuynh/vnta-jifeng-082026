namespace Vnta.Hrm.Application.KhauTru.KhauTruTamUng;

public sealed record PayrollAdvanceDeductionListItemDto(
    Guid PayrollDeductionSummaryRecordId,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal DeductionAmount,
    bool IsSummaryLocked,
    bool IsLocked,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
