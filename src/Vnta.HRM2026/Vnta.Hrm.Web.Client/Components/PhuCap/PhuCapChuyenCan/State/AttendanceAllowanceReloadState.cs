using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;

/// <summary>Ảnh chụp bất biến của bộ lọc tại thời điểm yêu cầu tải dữ liệu.</summary>
internal sealed record AttendanceAllowanceReloadSnapshot(
    int PayrollMonth,
    int PayrollYear,
    string? SearchText,
    AttendanceAllowanceLockState LockState,
    AttendanceAllowanceClass? AttendanceClass,
    int PageIndex,
    int PageSize);
