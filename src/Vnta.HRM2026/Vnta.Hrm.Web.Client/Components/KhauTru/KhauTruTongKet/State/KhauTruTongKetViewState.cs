using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.State;

/// <summary>
/// Immutable state contracts passed from the page coordinator to presentational sections.
/// Sections use callbacks to request work and never resolve a provider or HTTP service.
/// </summary>
public sealed record KhauTruTongKetGridState(
    IReadOnlyList<PayrollDeductionSummaryRecord> Records,
    IReadOnlyList<object> SelectedDataItems,
    string? SearchText,
    PayrollDeductionSummaryLockStatusCounts LockStatusCounts,
    string SelectedLockStatusKey,
    decimal VisibleDeductionTotal,
    int CurrentPageIndex,
    int PageSize,
    IReadOnlyList<int> PageSizeOptions,
    int TotalPageCount,
    int TotalRecordCount,
    string PagerSummaryText,
    string EmptyStateTitle,
    string EmptyStateMessage,
    string EmptyStateActionText,
    bool CanChangeFilters,
    bool CanOperateOnCurrentDataset,
    bool CanBrowsePages,
    bool CanEditRows,
    bool CanRefreshRows,
    bool CanViewMonthlyWork);

public static class KhauTruTongKetLockStatusKeys
{
    public const string All = "all";
    public const string Open = "open";
    public const string Locked = "locked";
}
