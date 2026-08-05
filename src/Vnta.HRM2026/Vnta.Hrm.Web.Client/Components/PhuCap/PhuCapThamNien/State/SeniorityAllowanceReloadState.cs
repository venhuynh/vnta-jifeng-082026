namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Ảnh chụp bất biến của bộ lọc tại thời điểm yêu cầu tải pager.</summary>
internal sealed record SeniorityAllowanceReloadSnapshot(
    int PayrollMonth,
    int PayrollYear,
    string? SearchText,
    string SeniorityRangeKey,
    int PageIndex,
    int PageSize);
