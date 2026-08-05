using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;

/// <summary>Popup trình bày bảng công tháng phục vụ đối chiếu Tổng kết khấu trừ.</summary>
public partial class KhauTruTongKetMonthlyWorkPopup
{
    private const string AllSummaryKey = "";
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly Dictionary<string, object> DecorativeIconButtonAttributes = new()
    {
        ["aria-hidden"] = "true",
        ["tabindex"] = "-1"
    };

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string Title { get; set; } = "Đối chiếu bảng công chi tiết";
    [Parameter] public string Context { get; set; } = string.Empty;
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public IReadOnlyList<MonthlyWorkdayPopupRow> Rows { get; set; } = [];
    [Parameter] public EventCallback RefreshRequested { get; set; }

    private string SelectedDayTypeSummaryKey { get; set; } = AllSummaryKey;
    private string SelectedStatusSummaryKey { get; set; } = AllSummaryKey;
    private string? SearchText { get; set; }
    private bool WasVisible { get; set; }
    private bool IsCompactLayout { get; set; }
    private decimal AdministrativeWorkdays => Rows.Count(row => row.IsRegularWorkday && row.HasCheckInOrOut);
    private decimal CalculatedSalaryWorkdays => AdministrativeWorkdays - (LateEarlyMinutesTotal / 480m);
    private int OvertimeMinutes15 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes15));
    private int OvertimeMinutes20 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes20));
    private int OvertimeMinutes30 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes30));
    private int TotalOvertimeMinutes => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes));
    private int LateEarlyMinutesTotal => Rows.Sum(row => row.LateEarlyTotalMinutes);
    private string SourceLockText => Rows.Any(row => row.IsLocked) ? "Có ngày công đã khóa" : "Chưa khóa công & tăng ca";
    private string SourceLockCssClass => Rows.Any(row => row.IsLocked) ? "is-locked" : "is-unlocked";
    private IReadOnlyList<MonthlyWorkdayPopupRow> FilteredRows => Rows
        .Where(row => MatchesSummary(row.DayType, SelectedDayTypeSummaryKey))
        .Where(row => MatchesSummary(row.Status, SelectedStatusSummaryKey))
        .ToArray();
    private IReadOnlyList<MonthlyWorkSummaryBadge> DayTypeSummaryBadges => BuildBadges(Rows.Select(row => row.DayType), GetDayTypeShortLabel);
    private IReadOnlyList<MonthlyWorkSummaryBadge> StatusSummaryBadges => BuildBadges(Rows.Select(row => row.Status), static key => key);
    private string PopupWidth => IsCompactLayout ? "calc(100vw - 0.5rem)" : "min(90rem, calc(100vw - 1rem))";
    private string PopupHeight => IsCompactLayout ? "min(34rem, calc(100vh - 0.5rem))" : "min(38rem, calc(100vh - 1rem))";
    private IReadOnlyList<MonthlyWorkSummaryCard> SummaryCards =>
    [
        new("Công hành chính", FormatWorkday(AdministrativeWorkdays), VntaDevExpressIcons.WorkCalendar, ButtonRenderStyle.Primary),
        new("Công tính lương", FormatSalaryWorkday(CalculatedSalaryWorkdays), VntaDevExpressIcons.CalculatorCoins, ButtonRenderStyle.Success),
        new("Tăng ca 1.5", FormatHours(OvertimeMinutes15), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Info),
        new("Tăng ca 2.0", FormatHours(OvertimeMinutes20), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Warning),
        new("Tăng ca 3.0", FormatHours(OvertimeMinutes30), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Danger),
        new("Tổng giờ tăng ca", FormatHours(TotalOvertimeMinutes), VntaDevExpressIcons.SummaryTotalEmployees, ButtonRenderStyle.Primary),
        new("Đi trễ/về sớm", LateEarlyMinutesTotal.ToString("N0", VietnameseCulture), VntaDevExpressIcons.SummaryLate, ButtonRenderStyle.Warning)
    ];

    protected override void OnParametersSet()
    {
        if(Visible && !WasVisible)
        {
            SelectedDayTypeSummaryKey = AllSummaryKey;
            SelectedStatusSummaryKey = AllSummaryKey;
            SearchText = null;
        }

        WasVisible = Visible;
        base.OnParametersSet();
    }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    private Task RefreshAsync() => RefreshRequested.InvokeAsync();

    private Task OnCompactLayoutChangedAsync(bool isCompactLayout)
    {
        IsCompactLayout = isCompactLayout;
        return InvokeAsync(StateHasChanged);
    }

    private Task SelectDayTypeSummaryAsync(string key) => SelectSummaryAsync(key, isDayType: true);
    private Task SelectStatusSummaryAsync(string key) => SelectSummaryAsync(key, isDayType: false);

    private Task SelectSummaryAsync(string key, bool isDayType)
    {
        if(isDayType)
        {
            SelectedDayTypeSummaryKey = key;
        }
        else
        {
            SelectedStatusSummaryKey = key;
        }

        return InvokeAsync(StateHasChanged);
    }

    private static IReadOnlyList<MonthlyWorkSummaryBadge> BuildBadges(IEnumerable<string> values, Func<string, string> shortLabelFactory)
    {
        var normalizedValues = values.Select(NormalizeSummaryKey).ToArray();
        return [
            new(AllSummaryKey, "Tất cả", "Tất cả", normalizedValues.Length),
            .. normalizedValues
                .GroupBy(value => value)
                .OrderBy(group => group.Key, StringComparer.CurrentCulture)
                .Select(group => new MonthlyWorkSummaryBadge(group.Key, shortLabelFactory(group.Key), group.Key, group.Count()))
        ];
    }

    private static bool MatchesSummary(string? value, string key) =>
        string.IsNullOrEmpty(key) || string.Equals(NormalizeSummaryKey(value), key, StringComparison.Ordinal);

    private static string NormalizeSummaryKey(string? value) => string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
    private static string GetDayTypeShortLabel(string key) => key switch { "Ngày thường" => "Thường", "Ngày nghỉ" => "Nghỉ", "Ngày lễ" => "Lễ", _ => key };
    private static string FormatWorkday(decimal value) => value.ToString("0.##", VietnameseCulture);
    private static string FormatSalaryWorkday(decimal value) => value.ToString("0.0", VietnameseCulture);
    private static string FormatHours(int minutes) => (Math.Max(0, minutes) / 60m).ToString("0.##", VietnameseCulture);
    private static string FormatOptionalHours(int minutes) => minutes <= 0 ? string.Empty : FormatHours(minutes);
    private static string FormatOptionalMinutes(int minutes) => minutes <= 0 ? string.Empty : minutes.ToString("N0", VietnameseCulture);
    private static string GetWeekdayDisplay(DateOnly workDate) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(workDate.ToDateTime(TimeOnly.MinValue).ToString("dddd", VietnameseCulture));
    private static string GetCheckTimeDisplay(MonthlyWorkdayPopupRow row)
    {
        var values = new[] { row.CheckInAt, row.CheckOutAt }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        return values.Length == 0 ? "--" : string.Join(" - ", values);
    }

    private sealed record MonthlyWorkSummaryCard(string Title, string ValueText, string IconUrl, ButtonRenderStyle RenderStyle)
    {
        public string CssClass => "deduction-summary-monthly-work-summary-card";
    }

    private sealed record MonthlyWorkSummaryBadge(string Key, string ShortLabel, string Label, int Count);
}
