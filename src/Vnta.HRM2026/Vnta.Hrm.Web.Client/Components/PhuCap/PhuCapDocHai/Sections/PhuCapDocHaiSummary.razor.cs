using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Sections;

public partial class PhuCapDocHaiSummary
{
    [Parameter] public IReadOnlyList<PhuCapDocHaiSummaryBadge> Badges { get; set; } = [];
    [Parameter] public string ActiveKey { get; set; } = string.Empty;
    [Parameter] public bool Enabled { get; set; }
    [Parameter] public string FormattedAllowanceTotal { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> BadgeSelected { get; set; }
}
