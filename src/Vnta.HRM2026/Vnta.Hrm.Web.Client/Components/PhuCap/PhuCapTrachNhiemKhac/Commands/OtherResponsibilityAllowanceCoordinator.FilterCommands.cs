namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

public sealed partial class OtherResponsibilityAllowanceCoordinator
{
    private async Task OnViewRequestedAsync()
    {
        if (!CanView) return;
        NormalizeSelectedPeriod();
        DataLoadErrorMessage = null;
        await ClearGridSelectionAsync();
        AppliedMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        HasRequestedData = true;
        await ReloadAsync();
    }

    private Task OnSelectedYearChangedAsync(int value)
    {
        ToolbarYear = Math.Clamp(value, MinimumSupportedYear, MaximumSupportedYear);
        ToolbarMonth = Math.Clamp(ToolbarMonth, GetMinimumSupportedMonth(ToolbarYear), 12);
        return Task.CompletedTask;
    }

    private Task OnSelectedMonthChangedAsync(int value)
    {
        ToolbarMonth = Math.Clamp(value, GetMinimumSupportedMonth(ToolbarYear), 12);
        return Task.CompletedTask;
    }

    private Task OnRetryAsync() => !HasRequestedData || HasPendingPeriodChange ? OnViewRequestedAsync() : ReloadAsync();

    private Task OnEmptyStateActionClick()
    {
        if (!HasRequestedData || HasPendingPeriodChange) return OnViewRequestedAsync();
        if (HasActiveSearch) SearchText = null;
        return ReloadAsync();
    }

    private Task OnSearchTextChanged(string? value)
    {
        var normalizedSearchText = NormalizeOptionalText(value);
        if (string.Equals(SearchText, normalizedSearchText, StringComparison.Ordinal)) return Task.CompletedTask;
        SearchText = normalizedSearchText;
        return ShouldReloadAfterSearchChange() ? ReloadAsync() : Task.CompletedTask;
    }

    private async Task OnPageSizeChanged(int value)
    {
        if (PageSize == value) return;
        IsChangingPageSize = true;
        CurrentLoadingText = "Đang cập nhật số dòng hiển thị...";
        PageSize = value;
        try
        {
            await RequestRenderAsync();
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
            ResetLoadingText();
        }
    }

    private void NormalizeSelectedPeriod()
    {
        ToolbarYear = Math.Clamp(ToolbarYear, MinimumSupportedYear, MaximumSupportedYear);
        ToolbarMonth = Math.Clamp(ToolbarMonth, GetMinimumSupportedMonth(ToolbarYear), 12);
    }

    private bool ShouldReloadAfterSearchChange() => HasRequestedData && !HasPendingPeriodChange;
}
