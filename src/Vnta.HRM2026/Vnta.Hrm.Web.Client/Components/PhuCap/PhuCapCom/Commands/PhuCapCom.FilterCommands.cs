using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Owns period, search and paging commands for the meal-allowance screen.</summary>
public partial class PhuCapCom
{
    private async Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeNullable(value);
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
            return;

        SearchText = normalizedValue;
        currentPageIndex = 0;
        if(!HasRequestedData || HasPendingPeriodChange)
            return;

        await ReloadAsync();
    }

    private Task OnSelectedMonthChangedAsync(int value)
    {
        if(ToolbarMonth == value)
            return Task.CompletedTask;

        var normalizedPeriod = NormalizeSelectedPeriod(value, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        InvalidateReloadForPendingPeriodChange();
        return Task.CompletedTask;
    }

    private Task OnSelectedYearChangedAsync(int value)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, value);
        if(ToolbarMonth == normalizedPeriod.Month && ToolbarYear == normalizedPeriod.Year)
            return Task.CompletedTask;

        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        InvalidateReloadForPendingPeriodChange();
        return Task.CompletedTask;
    }

    private async Task OnApplyPeriodFilterClick()
    {
        if(!CanReload)
            return;

        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        LoadErrorMessage = null;

        await ClearSelectionAsync();
        AppliedMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        currentPageIndex = 0;
        HasRequestedData = true;
        await ReloadAsync();
    }

    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Any(option => option.Value == value)
            ? value
            : PageSizeOptions[0].Value;
        if(normalizedValue == AllPageSize && totalRecordCount > AllPageSize)
        {
            ToastService.ShowWarning($"Chỉ có thể hiển thị tất cả khi kỳ lương có tối đa {AllPageSize:N0} dòng.");
            return;
        }

        if(PageSize == normalizedValue)
            return;

        IsChangingPageSize = true;
        LoadingText = "Đang cập nhật số dòng hiển thị...";
        try
        {
            var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
            pageSize = normalizedValue;
            currentPageIndex = firstVisibleRecordIndex / pageSize;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            if(HasRequestedData && !HasPendingPeriodChange)
                await ReloadAsync();
        }
        finally
        {
            IsChangingPageSize = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if(!CanBrowsePages)
            return;

        var normalizedValue = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if(normalizedValue == CurrentPageIndex)
            return;

        currentPageIndex = normalizedValue;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task OnEmptyStateActionClick()
    {
        if(!HasRequestedData)
        {
            await OnApplyPeriodFilterClick();
            return;
        }

        if(CanResetFilters)
        {
            await ResetFiltersAsync();
            return;
        }

        if(HasPendingPeriodChange)
        {
            AppliedMonth = ToolbarMonth;
            AppliedYear = ToolbarYear;
            await ReloadAsync();
            return;
        }

        await ReloadAsync();
    }

    private Task OnRetryAsync() =>
        !HasRequestedData || HasPendingPeriodChange
            ? OnApplyPeriodFilterClick()
            : ReloadAsync();
}
