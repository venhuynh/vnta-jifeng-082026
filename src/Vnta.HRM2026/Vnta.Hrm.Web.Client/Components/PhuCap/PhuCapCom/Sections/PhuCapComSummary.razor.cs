using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

public partial class PhuCapComSummary
{
    [Parameter] public MealAllowanceSummaryDto Summary { get; set; } = new(0, 0, 0, 0, 0, 0, 0, 0m);
    [Parameter] public string SelectedKey { get; set; } = string.Empty;
    [Parameter] public string AllKey { get; set; } = string.Empty;
    [Parameter] public string WithAllowanceKey { get; set; } = string.Empty;
    [Parameter] public string WithoutAllowanceKey { get; set; } = string.Empty;
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public Func<decimal, string> FormatMoney { get; set; } = value => value.ToString("N0");
    [Parameter] public EventCallback<string> SummarySelected { get; set; }
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
}
