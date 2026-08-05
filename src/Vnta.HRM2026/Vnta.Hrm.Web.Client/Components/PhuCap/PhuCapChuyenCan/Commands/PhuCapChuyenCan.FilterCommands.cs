using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan;

public partial class PhuCapChuyenCan
{
    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedDataItemsChanged</c>.</summary>
    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSearchTextChanged</c>.</summary>
    private Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeNullable(value);
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchText = normalizedValue;
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            return Task.CompletedTask;
        }

        currentPageIndex = 0;
        return ReloadAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSummaryBadgeClick</c>.</summary>
    private async Task OnSummaryBadgeClick(string badgeKey)
    {
        if(!CanInteract || string.Equals(badgeKey, ActiveSummaryBadgeKey, StringComparison.Ordinal))
        {
            return;
        }

        ActiveSummaryBadgeKey = badgeKey;
        currentPageIndex = 0;
        if(HasRequestedData && !HasPendingPeriodChange)
        {
            await ReloadAsync();
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnPageSizeChanged</c>.</summary>
    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Any(option => option.Value == value)
            ? value
            : PageSizeOptions[0].Value;
        if(normalizedValue == AllPageSize && TotalRecordCount > AllPageSize)
        {
            ToastService.ShowWarning($"Chỉ có thể hiển thị tất cả khi kỳ lương có tối đa {AllPageSize:N0} dòng.");
            return;
        }

        if(PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        CurrentLoadingText = "Đang cập nhật số dòng hiển thị...";

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
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnActivePageIndexChangedAsync</c>.</summary>
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

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedMonthChangedAsync</c>.</summary>
    private async Task OnSelectedMonthChangedAsync(int month)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(month, ToolbarYear);
        if(normalizedPeriod.Month == ToolbarMonth && normalizedPeriod.Year == ToolbarYear)
        {
            return;
        }

        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        await HandleToolbarPeriodDraftChangedAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedYearChangedAsync</c>.</summary>
    private async Task OnSelectedYearChangedAsync(int year)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, year);
        if(normalizedPeriod.Month == ToolbarMonth && normalizedPeriod.Year == ToolbarYear)
        {
            return;
        }

        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        await HandleToolbarPeriodDraftChangedAsync();
    }

    /// <summary>Thực hiện xử lý cho luồng <c>HandleToolbarPeriodDraftChangedAsync</c>.</summary>
    private async Task HandleToolbarPeriodDraftChangedAsync()
    {
        if(!HasRequestedData)
        {
            return;
        }

        Interlocked.Increment(ref ReloadLifecycleState.RequestedVersion);
        CancelActiveReload();
        Records = [];
        totalRecordCount = 0;
        HasLoadError = false;
        await ClearSelectionAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnViewRequestedAsync</c>.</summary>
    private async Task OnViewRequestedAsync()
    {
        if(!CanView)
        {
            return;
        }

        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        AppliedMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        currentPageIndex = 0;
        HasRequestedData = true;
        await ReloadAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnRetryAsync</c>.</summary>
    private Task OnRetryAsync() =>
        !HasRequestedData || HasPendingPeriodChange
            ? OnViewRequestedAsync()
            : ReloadAsync();
}

