namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

/// <summary>Sở hữu riêng trạng thái filter của lưới tiến độ triển khai.</summary>
internal sealed class ProjectImplementationProgressFilterState
{
    internal string ActiveSummaryBadgeKey { get; private set; } = ProjectImplementationProgressStatusCatalog.AllKey;

    internal string? SearchText { get; private set; }

    internal void SetSearchText(string? value) =>
        SearchText = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal void SelectSummaryBadge(string key) =>
        ActiveSummaryBadgeKey = ProjectImplementationProgressStatusCatalog.IsFilterKey(key)
            ? key
            : ProjectImplementationProgressStatusCatalog.AllKey;

    internal void Reset()
    {
        ActiveSummaryBadgeKey = ProjectImplementationProgressStatusCatalog.AllKey;
        SearchText = null;
    }
}
