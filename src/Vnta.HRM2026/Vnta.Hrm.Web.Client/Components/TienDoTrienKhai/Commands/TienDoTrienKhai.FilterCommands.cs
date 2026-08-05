namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Xử lý các thao tác filter thuần local-only của màn hình tiến độ.</summary>
public partial class TienDoTrienKhai
{
    private Task OnSearchTextChanged(string? value)
    {
        SessionState.SetSearchText(value);
        return Task.CompletedTask;
    }

    private Task OnSummaryBadgeClick(string badgeKey)
    {
        SessionState.SelectSummaryBadge(badgeKey);
        return Task.CompletedTask;
    }

    private Task OnActivePageIndexChangedAsync(int pageIndex)
    {
        SessionState.SetCurrentPageIndex(pageIndex);
        return Task.CompletedTask;
    }

    private Task OnPageSizeChangedAsync(int pageSize)
    {
        SessionState.SetPageSize(pageSize);
        return Task.CompletedTask;
    }
}
