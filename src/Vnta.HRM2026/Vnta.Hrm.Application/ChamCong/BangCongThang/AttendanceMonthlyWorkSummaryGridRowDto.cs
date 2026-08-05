namespace Vnta.Hrm.Application.ChamCong.BangCongThang;

/// <summary>
/// Một row đại diện một nhân viên; day-cell được tách để paging không bị nhân theo số ngày công.
/// </summary>
public sealed record AttendanceMonthlyWorkSummaryGridRowDto(
    Guid EmployeeId,
    int RowNumber,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    IReadOnlyList<AttendanceMonthlyWorkSummaryDayCellDto> DayCells);
