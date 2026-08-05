using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Owns selection transitions and screen-wide busy-state coordination.</summary>
public partial class PhuCapTrachNhiem
{
    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        await (GridSection?.ClearSelectionAsync() ?? Task.CompletedTask);
    }

    private List<PayrollResponsibilityAllowanceAbcItemDto> GetSelectedAbcRows()
    {
        var visibleIds = AbcRows.Select(row => row.Id).ToHashSet();
        return SelectedDataItems
            .OfType<PayrollResponsibilityAllowanceAbcItemDto>()
            .Where(row => visibleIds.Contains(row.Id))
            .DistinctBy(row => row.Id)
            .ToList();
    }

    private void ApplyUpdatedAbcRow(PayrollResponsibilityAllowanceAbcItemDto updatedRow)
    {
        AbcRows = AbcRows
            .Select(row => row.Id == updatedRow.Id ? updatedRow : row)
            .ToArray();
        SelectedDataItems = SelectedDataItems
            .Select(item => item is PayrollResponsibilityAllowanceAbcItemDto row && row.Id == updatedRow.Id
                ? updatedRow
                : item)
            .ToArray();
        ClampCurrentPageIndex();
    }

    private void ClampCurrentPageIndex() =>
        currentPageIndex = Math.Clamp(currentPageIndex, 0, Math.Max(0, TotalPageCount - 1));

    private static bool IsWholePeriodLockActionScope(string scope) =>
        string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);

    private string BuildLockActionLoadingText(string actionText, string scope, int selectedCount) =>
        IsWholePeriodLockActionScope(scope)
            ? $"Đang {actionText} dữ liệu phụ cấp trách nhiệm của toàn bộ kỳ {PendingLockActionPeriodLabel}..."
            : $"Đang {actionText} phụ cấp trách nhiệm của {selectedCount:N0} nhân viên đã chọn...";

    private async Task RunBusyAsync(string loadingText, Func<Task> action)
    {
        IsExecutingCommand = true;
        CurrentCommandLoadingText = loadingText;

        try
        {
            await action();
        }
        finally
        {
            IsExecutingCommand = false;
            CurrentCommandLoadingText = HrmUiDefaults.LoadingText;
        }
    }
}
