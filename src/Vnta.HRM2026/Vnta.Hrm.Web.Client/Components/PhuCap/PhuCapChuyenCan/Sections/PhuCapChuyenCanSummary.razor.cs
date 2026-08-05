using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Sections;

/// <summary>Trình bày các chỉ số tổng hợp và bộ lọc tìm kiếm của màn hình.</summary>
public partial class PhuCapChuyenCanSummary
{
    [Parameter] public IReadOnlyList<AttendanceAllowanceSummaryBadge> SummaryBadges { get; set; } = [];
    [Parameter] public string ActiveSummaryBadgeKey { get; set; } = string.Empty;
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public decimal VisibleActualAllowanceTotal { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public Func<decimal, string> FormatMoney { get; set; } = value => value.ToString("N0");
    [Parameter] public EventCallback<string> SummaryBadgeSelected { get; set; }
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
}
