namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien;

public partial class PhuCapTrachNhiemGanNhanVien
{
    private async Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeOptional(value);
        if (string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedValue;
        if (!HasRequestedData || HasPendingPeriodChange)
        {
            return;
        }

        CurrentPageIndex = 0;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task OnSelectedMonthChangedAsync(int month)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(month, ToolbarYear);
        if (normalizedPeriod.Month == ToolbarMonth && normalizedPeriod.Year == ToolbarYear)
        {
            return;
        }

        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        await HandleToolbarPeriodDraftChangedAsync();
    }

    private async Task OnSelectedYearChangedAsync(int year)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, year);
        if (normalizedPeriod.Month == ToolbarMonth && normalizedPeriod.Year == ToolbarYear)
        {
            return;
        }

        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        await HandleToolbarPeriodDraftChangedAsync();
    }

    private async Task SelectGradePresenceAsync(string gradePresenceKey)
    {
        if (!CanInteract || string.Equals(SelectedGradePresenceKey, gradePresenceKey, StringComparison.Ordinal))
        {
            return;
        }

        SelectedGradePresenceKey = gradePresenceKey;
        CurrentPageIndex = 0;
        await ClearSelectionAsync();
        if (HasRequestedData && !HasPendingPeriodChange)
        {
            await ReloadAsync();
        }
    }

    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Contains(value) ? value : PageSizeOptions[0];
        if (PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
        PageSize = normalizedValue;
        CurrentPageIndex = firstVisibleRecordIndex / PageSize;
        try
        {
            await ClearSelectionAsync();
            if (HasRequestedData && !HasPendingPeriodChange)
            {
                await ReloadAsync();
            }
        }
        finally
        {
            IsChangingPageSize = false;
        }
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectionState.Items = items;
        return Task.CompletedTask;
    }

    private async Task OnActivePageIndexChangedAsync(int pageIndex)
    {
        if (!CanBrowsePages)
        {
            return;
        }

        var normalizedPageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, PageCount - 1));
        if (CurrentPageIndex == normalizedPageIndex)
        {
            return;
        }

        CurrentPageIndex = normalizedPageIndex;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private Task OnRetryAsync() =>
        !HasRequestedData || HasPendingPeriodChange ? LoadPeriodAsync() : ReloadAsync();

    private async Task HandleToolbarPeriodDraftChangedAsync()
    {
        if (!HasRequestedData)
        {
            return;
        }

        Interlocked.Increment(ref ReloadLifecycleState.RequestedVersion);
        CancelActiveReload();
        ClearPage();
        LoadErrorMessage = null;
        CurrentPageIndex = 0;
        await ClearSelectionAsync();
        await InvokeAsync(StateHasChanged);
    }

}
