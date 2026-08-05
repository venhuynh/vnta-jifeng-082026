using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

public partial class PhuCapTongHopSummary
{
    [Parameter] public IReadOnlyList<AllowanceSummaryBadge> SummaryBadges { get; set; } = [];
    [Parameter] public string ActiveSummaryBadgeKey { get; set; } = string.Empty;
    [Parameter] public IReadOnlyList<AllowanceAmountSummary> VisibleAllowanceSummaries { get; set; } = [];
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter] public Func<decimal, string> FormatSummaryMoney { get; set; } = value => value.ToString("N0");
    [Parameter] public EventCallback<string> SummaryBadgeRequested { get; set; }
    [Parameter] public EventCallback<string?> SearchChanged { get; set; }

    private static string GetBadgeCssClass(string badgeKey) =>
        $"attendance-allowance-summary-button attendance-allowance-summary-button-{badgeKey}";
}
