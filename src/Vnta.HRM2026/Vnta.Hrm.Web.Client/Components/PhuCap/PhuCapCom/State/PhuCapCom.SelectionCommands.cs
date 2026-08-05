using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Owns grid selection, summary buckets and period selection state transitions.</summary>
public partial class PhuCapCom
{
    private async Task OnRulesClick()
    {
        IsRulesPopupVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    private Task OnColumnChooserRequested()
    {
        Grid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private async Task OnGridFilterCriteriaChangedAsync(GridFilterCriteriaChangedEventArgs _)
    {
        await ClearSelectionAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectAllowanceSummaryAsync(string summaryKey)
    {
        if(!CanInteract || string.Equals(SelectedAllowanceSummaryKey, summaryKey, StringComparison.Ordinal))
            return;

        SelectedAllowanceSummaryKey = summaryKey;
        currentPageIndex = 0;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task ResetFiltersAsync()
    {
        await ClearSelectionAsync();
        ToolbarMonth = DefaultReferenceMonth;
        ToolbarYear = DefaultReferenceYear;
        SearchText = null;
        AppliedMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        currentPageIndex = 0;
        await ReloadAsync();
    }

    private IReadOnlyList<MealAllowanceRecord> GetSelectedRecords()
    {
        var selectedIds = SelectedDataItems
            .OfType<MealAllowanceRecord>()
            .Select(record => record.Id)
            .ToHashSet();

        return Records.Where(record => selectedIds.Contains(record.Id)).ToArray();
    }

    private Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        return Task.CompletedTask;
    }

    private async Task PruneSelectionToVisibleRecordsAsync()
    {
        if(SelectedDataItems.Count == 0)
            return;

        var visibleIds = Records.Select(record => record.Id).ToHashSet();
        var visibleSelection = SelectedDataItems
            .OfType<MealAllowanceRecord>()
            .Where(record => visibleIds.Contains(record.Id))
            .DistinctBy(record => record.Id)
            .Cast<object>()
            .ToArray();

        if(visibleSelection.Length == SelectedDataItems.Count)
            return;

        SelectedDataItems = visibleSelection;
        if(visibleSelection.Length == 0)
            Grid?.SetFocusedRowIndex(-1);

        await InvokeAsync(StateHasChanged);
    }

    private void InvalidateReloadForPendingPeriodChange()
    {
        if(!HasRequestedData)
            return;

        Interlocked.Increment(ref ReloadState.RequestedVersion);
        CancelActiveReload();
    }

    private (int Month, int Year) GetAppliedPayrollPeriod()
    {
        if(AppliedMonth is not { } appliedMonth || AppliedYear is not { } appliedYear)
            throw new InvalidOperationException("Chưa có kỳ lương đã áp dụng để thực hiện thao tác phụ cấp cơm.");

        return (appliedMonth, appliedYear);
    }

    private static bool IsWholePeriodLockActionScope(string scope) =>
        string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);
}
