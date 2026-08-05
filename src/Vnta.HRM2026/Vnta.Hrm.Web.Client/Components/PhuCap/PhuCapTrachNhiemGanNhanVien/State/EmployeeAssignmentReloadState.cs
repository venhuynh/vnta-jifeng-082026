namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.State;

/// <summary>Ảnh chụp bộ lọc tại thời điểm yêu cầu tải dữ liệu.</summary>
internal sealed record EmployeeAssignmentReloadSnapshot(
    int PayrollYear,
    int PayrollMonth,
    string? SearchText,
    string? GradePresenceKey,
    int PageIndex,
    int PageSize);
