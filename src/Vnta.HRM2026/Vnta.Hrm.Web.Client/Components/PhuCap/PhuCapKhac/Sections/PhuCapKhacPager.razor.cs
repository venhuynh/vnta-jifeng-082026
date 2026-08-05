using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Sections;

public partial class PhuCapKhacPager
{
    [Parameter, EditorRequired] public string SummaryText { get; set; } = string.Empty;
    [Parameter] public bool CanBrowsePages { get; set; }
    [Parameter] public int TotalPageCount { get; set; }
    [Parameter] public int CurrentPageIndex { get; set; }
    [Parameter] public EventCallback<int> ActivePageIndexChanged { get; set; }
    [Parameter, EditorRequired] public IReadOnlyList<OtherAllowancePageSizeOption> PageSizeOptions { get; set; } = [];
    [Parameter] public int PageSize { get; set; }
    [Parameter] public EventCallback<int> PageSizeChanged { get; set; }
    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter, EditorRequired] public string PageSizeDescription { get; set; } = string.Empty;
}
