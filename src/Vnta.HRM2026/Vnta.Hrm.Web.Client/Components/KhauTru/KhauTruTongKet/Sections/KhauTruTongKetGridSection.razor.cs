using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.State;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;

/// <summary>
/// Presents the deduction-summary results and forwards every user intent to
/// the page coordinator. It owns only the DevExpress grid instance.
/// </summary>
public partial class KhauTruTongKetGridSection
{
    private const string DeductionAmountTotalSummaryName = "DeductionAmountTotal";
    private static readonly IReadOnlyList<LockStatusFilter> LockStatusFilters =
    [
        new(KhauTruTongKetLockStatusKeys.All, "Tất cả"),
        new(KhauTruTongKetLockStatusKeys.Open, "Đang mở"),
        new(KhauTruTongKetLockStatusKeys.Locked, "Đã khóa")
    ];

    private IGrid? grid;

    [Parameter, EditorRequired]
    public KhauTruTongKetGridState State { get; set; } = default!;

    [Parameter] public bool HasLoadError { get; set; }
    [Parameter] public string? LoadErrorMessage { get; set; }
    [Parameter] public bool ShowLoadingPanel { get; set; }
    [Parameter] public string LoadingText { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public Func<string?, MarkupString> HighlightSearchText { get; set; } = value => new MarkupString(value ?? string.Empty);

    [Parameter, EditorRequired]
    public Func<decimal, string> FormatMoney { get; set; } = value => value.ToString();

    [Parameter] public EventCallback RetryRequested { get; set; }
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
    [Parameter] public EventCallback<string> LockStatusSelected { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedDataItemsChanged { get; set; }
    [Parameter] public EventCallback<GridFilterCriteriaChangedEventArgs> FilterCriteriaChanged { get; set; }
    [Parameter] public EventCallback<PayrollDeductionSummaryRecord> EditRequested { get; set; }
    [Parameter] public EventCallback<PayrollDeductionSummaryRecord> RefreshRequested { get; set; }
    [Parameter] public EventCallback<PayrollDeductionSummaryRecord> ToggleLockRequested { get; set; }
    [Parameter] public EventCallback<PayrollDeductionSummaryRecord> MonthlyWorkRequested { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }
    [Parameter] public EventCallback<int> ActivePageIndexChanged { get; set; }
    [Parameter] public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>Opens the column chooser for the results grid.</summary>
    public void ShowColumnChooser() => grid?.ShowColumnChooser();

    public Task ExportSelectedToExcelAsync(string fileName) =>
        grid?.ExportToXlsxAsync(fileName, new GridXlExportOptions { ExportSelectedRowsOnly = true })
        ?? throw new InvalidOperationException("Lưới dữ liệu chưa sẵn sàng để xuất.");

    public Task ExportSelectedToPdfAsync(string fileName) =>
        grid?.ExportToPdfAsync(fileName, new GridPdfExportOptions { ExportSelectedRowsOnly = true })
        ?? throw new InvalidOperationException("Lưới dữ liệu chưa sẵn sàng để xuất.");

    /// <summary>
    /// Clears both the coordinator selection state and the grid's visual
    /// selection/focus, so commands cannot retain stale selected rows.
    /// </summary>
    public async Task ClearSelectionAsync()
    {
        await SelectedDataItemsChanged.InvokeAsync([]);

        if(grid is null)
        {
            return;
        }

        await grid.DeselectAllAsync();
        grid.SetFocusedRowIndex(-1);
    }

    /// <summary>Returns the current grid total after client-side filtering.</summary>
    public decimal GetVisibleDeductionTotal()
    {
        var summaryItem = grid?.GetTotalSummaryItems()
            .FirstOrDefault(item => string.Equals(item.Name, DeductionAmountTotalSummaryName, StringComparison.Ordinal));
        var summaryValue = summaryItem is null ? null : grid!.GetTotalSummaryValue(summaryItem);

        return summaryValue switch
        {
            decimal value => value,
            null => 0m,
            IConvertible value => Convert.ToDecimal(value, System.Globalization.CultureInfo.GetCultureInfo("vi-VN")),
            _ => 0m
        };
    }

    private int GetLockStatusCount(string lockStatusKey) => lockStatusKey switch
    {
        KhauTruTongKetLockStatusKeys.Open => State.LockStatusCounts.Open,
        KhauTruTongKetLockStatusKeys.Locked => State.LockStatusCounts.Locked,
        _ => State.LockStatusCounts.All
    };

    private bool CanEditRow(PayrollDeductionSummaryRecord record) => State.CanEditRows && !record.IsLocked;

    private bool CanRefreshRow(PayrollDeductionSummaryRecord record) => State.CanRefreshRows && !record.IsLocked;

    private bool CanViewMonthlyWork(PayrollDeductionSummaryRecord record) =>
        State.CanViewMonthlyWork
        && record.EmployeeId != Guid.Empty;

    private static string GetLockStatusFilterCssClass(string lockStatusKey) =>
        $"deduction-summary-summary-button deduction-summary-summary-button-{lockStatusKey}";

    private static string GetLockBadgeCssClass(bool isLocked) =>
        isLocked
            ? "yes-no-status yes-no-status-no hrm-grid-status"
            : "yes-no-status yes-no-status-yes hrm-grid-status";

    private static string GetNotePreview(string? note) => note ?? string.Empty;

    private sealed record LockStatusFilter(string Key, string Label);
}
