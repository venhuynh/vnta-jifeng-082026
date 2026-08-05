using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Owns grid callbacks, selection, totals, and paging state transitions.</summary>
public partial class PhuCapTongHop
{
    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private Task OnGridFilterCriteriaChangedAsync(GridFilterCriteriaChangedEventArgs _)
    {
        IsAllowanceTotalsSyncPending = true;
        return InvokeAsync(StateHasChanged);
    }

    private void ResetVisibleAllowanceTotals() =>
        VisibleAllowanceTotals = new AllowanceAmountTotals(
            Records.Sum(record => record.TotalAllowanceAmount), Records.Sum(record => record.ResponsibilityAllowanceAmount),
            Records.Sum(record => record.ResponsibilityOtherAllowanceAmount), Records.Sum(record => record.SeniorityAllowanceAmount),
            Records.Sum(record => record.AttendanceAllowanceAmount), Records.Sum(record => record.MealAllowanceAmount),
            Records.Sum(record => record.HazardAllowanceAmount), Records.Sum(record => record.OtherAllowanceAmount),
            Records.Sum(record => record.LeaveHolidayAllowanceAmount));

    private void UpdateVisibleAllowanceTotalsFromGrid() =>
        VisibleAllowanceTotals = new AllowanceAmountTotals(
            GetGridTotal(TotalAllowanceAmountTotalSummaryName), GetGridTotal(ResponsibilityAllowanceAmountTotalSummaryName),
            GetGridTotal(ResponsibilityOtherAllowanceAmountTotalSummaryName), GetGridTotal(SeniorityAllowanceAmountTotalSummaryName),
            GetGridTotal(AttendanceAllowanceAmountTotalSummaryName), GetGridTotal(MealAllowanceAmountTotalSummaryName),
            GetGridTotal(HazardAllowanceAmountTotalSummaryName), GetGridTotal(OtherAllowanceAmountTotalSummaryName),
            GetGridTotal(LeaveHolidayAllowanceAmountTotalSummaryName));

    private decimal GetGridTotal(string summaryName)
    {
        var summaryItem = Grid?.GetTotalSummaryItems().FirstOrDefault(item => string.Equals(item.Name, summaryName, StringComparison.Ordinal));
        var summaryValue = summaryItem is null ? null : Grid!.GetTotalSummaryValue(summaryItem);
        return summaryValue switch { decimal value => value, null => 0m, IConvertible value => Convert.ToDecimal(value, DisplayCulture), _ => 0m };
    }

    private Task OnColumnChooserRequested()
    {
        Grid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Any(option => option.Value == value)
            ? value
            : PageSizeOptions[0].Value;
        if(PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        try
        {
            var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
            pageSize = normalizedValue;
            currentPageIndex = firstVisibleRecordIndex / pageSize;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            if(HasRequestedData && !HasPendingPeriodChange)
            {
                await ReloadAsync();
            }
        }
        finally
        {
            IsChangingPageSize = false;
        }
    }

    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if(!CanBrowsePages)
        {
            return;
        }

        var normalizedValue = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if(normalizedValue == currentPageIndex)
        {
            return;
        }

        currentPageIndex = normalizedValue;
        await ReloadAsync();
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        if(Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private List<PayrollAllowanceSummaryRecord> GetSelectedRows()
    {
        var visibleRowsById = Records.ToDictionary(row => row.Id);
        return SelectedDataItems.OfType<PayrollAllowanceSummaryRecord>().Where(row => visibleRowsById.ContainsKey(row.Id))
            .Select(row => visibleRowsById[row.Id]).DistinctBy(row => row.Id).ToList();
    }
}
