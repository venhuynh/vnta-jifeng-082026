using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.SharedUi.Layout;

public partial class VntaDataListPageLayout
{
    [Parameter] public RenderFragment? ToolbarContent { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? RootClass { get; set; }
    [Parameter] public string? ToolbarClass { get; set; }
    [Parameter] public string? ContentClass { get; set; }
    [Parameter] public string? ContentCardClass { get; set; }

    private string RootCssClass => Css("vnta-list-page", RootClass);
    private string ToolbarCssClass => Css("vnta-list-page-card vnta-list-page-toolbar", ToolbarClass);
    private string ContentCssClass => Css("vnta-list-page-content", ContentClass);
    private string ContentCardCssClass => Css("vnta-list-page-card vnta-list-page-content-card", ContentCardClass);

    private static string Css(string baseClass, string? extraClass)
        => string.IsNullOrWhiteSpace(extraClass) ? baseClass : $"{baseClass} {extraClass}";
}
