using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Sections;

public partial class PhuCapTrachNhiemKhacResultsGrid
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private IGrid? grid;

    [Parameter, EditorRequired]
    public global::Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.OtherResponsibilityAllowanceResultsGridState State { get; set; } = default!;

    [Parameter] public EventCallback<IGrid?> GridChanged { get; set; }
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
    [Parameter] public EventCallback<int> PageSizeChanged { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedGridItemsChanged { get; set; }
    [Parameter] public EventCallback EmptyStateActionClick { get; set; }

    private bool ShowLoadingPanel => State.ShowLoadingPanel;
    private string LoadingText => State.LoadingText;
    private string PeriodHintCssClass => State.PeriodHintCssClass;
    private string PeriodHintText => State.PeriodHintText;
    private string? SearchText => State.SearchText;
    private bool CanSearchScreen => State.CanSearchScreen;
    private bool CanUseAppliedPeriodActions => State.CanUseAppliedPeriodActions;
    private IReadOnlyList<OtherResponsibilityAllowanceRecord> VisibleRecords => State.VisibleRecords;
    private int PageSize => State.PageSize;
    private IReadOnlyList<object> SelectedGridItems => State.SelectedGridItems;
    private string EmptyStateTitle => State.EmptyStateTitle;
    private string EmptyStateMessage => State.EmptyStateMessage;
    private string EmptyStateActionText => State.EmptyStateActionText;
    private bool CanEmptyStateAction => State.CanEmptyStateAction;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && grid is not null)
        {
            await GridChanged.InvokeAsync(grid);
        }
    }

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = FormatOptionalText(value);
        if (string.IsNullOrWhiteSpace(SearchText)) return new MarkupString(WebUtility.HtmlEncode(displayText));
        var searchText = SearchText.Trim();
        if (searchText.Length == 0) return new MarkupString(WebUtility.HtmlEncode(displayText));

        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);
        while (true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0) break;
            if (matchIndex > startIndex) builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"responsibility-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if (startIndex < displayText.Length) builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    private static string FormatOptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? "Chưa có" : value.Trim();
    private string FormatCurrency(decimal amount) => amount == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", amount);
    private string FormatWorkdayCount(decimal value) => value.ToString("0.0000", DisplayCulture);
    private static string GetYesNoStatusCssClass(bool value) => string.Join(' ', "yes-no-status", value ? "yes-no-status-yes" : "yes-no-status-no");
    private static string GetLockStatusText(bool isLocked) => isLocked ? "Khóa" : "Mở";
}
