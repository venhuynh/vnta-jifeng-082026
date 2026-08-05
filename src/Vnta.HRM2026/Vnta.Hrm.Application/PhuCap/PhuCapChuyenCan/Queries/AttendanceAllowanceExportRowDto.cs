namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

/// <summary>Allowlist dữ liệu phụ cấp chuyên cần được phép rời backend để tạo tệp xuất.</summary>
public sealed record AttendanceAllowanceExportRowDto(
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal ActualWorkdayCount,
    decimal StandardWorkdayCount,
    decimal AttendanceRate,
    decimal ActualAllowanceAmount,
    bool IsLocked,
    decimal AdministrativeWorkdayCount = 0m,
    decimal LateEarlyDeductionDays = 0m,
    decimal? CtlWorkdayCount = null,
    decimal? Kqcc = null,
    bool HasKpViolation = false)
{
    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear}";

    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";
}
