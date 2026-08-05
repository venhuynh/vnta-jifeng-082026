namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Owns period drafts, server filters, and empty-state reload commands.</summary>
public partial class PhuCapTongHop
{
    private async Task OnViewRequestedAsync()
    {
        if(!CanView) return;
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        AppliedMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        currentPageIndex = 0;
        HasRequestedData = true;
        await ReloadAsync();
    }

    private Task OnRetryAsync() => !HasRequestedData || HasPendingPeriodChange ? OnViewRequestedAsync() : ReloadAsync();

    private async Task OnEmptyStateActionClick()
    {
        if(!HasRequestedData)
        {
            await OnViewRequestedAsync();
            return;
        }
        await ReloadAsync();
    }

    private Task OnToolbarMonthChanged(int value)
    {
        var normalized = NormalizeSelectedPeriod(value, ToolbarYear);
        if(normalized.Month == ToolbarMonth && normalized.Year == ToolbarYear) return Task.CompletedTask;
        ToolbarMonth = normalized.Month;
        ToolbarYear = normalized.Year;
        HandleToolbarPeriodDraftChanged();
        return Task.CompletedTask;
    }

    private Task OnToolbarYearChanged(int value)
    {
        var normalized = NormalizeSelectedPeriod(ToolbarMonth, value);
        if(normalized.Month == ToolbarMonth && normalized.Year == ToolbarYear) return Task.CompletedTask;
        ToolbarMonth = normalized.Month;
        ToolbarYear = normalized.Year;
        HandleToolbarPeriodDraftChanged();
        return Task.CompletedTask;
    }

    private Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeOptional(value);
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal)) return Task.CompletedTask;
        SearchText = normalizedValue;
        if(!HasRequestedData || HasPendingPeriodChange) return Task.CompletedTask;
        currentPageIndex = 0;
        return ReloadAsync();
    }

    private async Task OnSummaryBadgeClick(string badgeKey)
    {
        if(string.Equals(badgeKey, ActiveSummaryBadgeKey, StringComparison.Ordinal)) return;
        ActiveSummaryBadgeKey = badgeKey;
        if(!HasRequestedData || HasPendingPeriodChange) return;
        currentPageIndex = 0;
        await ReloadAsync();
    }

    private void HandleToolbarPeriodDraftChanged()
    {
        Interlocked.Increment(ref ReloadState.RequestedVersion);
        CancelActiveReload();
    }
}
