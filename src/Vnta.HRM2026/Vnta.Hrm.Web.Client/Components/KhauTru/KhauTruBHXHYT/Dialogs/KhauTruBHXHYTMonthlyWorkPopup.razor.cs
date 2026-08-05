using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT;

public partial class KhauTruBHXHYTMonthlyWorkPopup
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
    [Parameter] public string Title { get; set; } = "Bảng công tháng";
    [Parameter] public string Context { get; set; } = string.Empty;
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public IReadOnlyList<KhauTruBHXHYTMonthlyWorkdayRow> Rows { get; set; } = [];
    [Parameter] public EventCallback RefreshRequested { get; set; }

    private string SelectedDayTypeSummaryKey { get; set; } = AllSummaryKey;
    private string SelectedStatusSummaryKey { get; set; } = AllSummaryKey;
    private string? SearchText { get; set; }
    private bool WasVisible { get; set; }
    private bool IsCompactLayout { get; set; }
    private int AttendedRegularWorkdays => Rows.Count(row => row.IsRegularWorkday && row.HasCheckInOrOut);
    private int OvertimeMinutes15 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes15));
    private int OvertimeMinutes20 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes20));
    private int OvertimeMinutes30 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes30));
    private int LateEarlyMinutesTotal => Rows.Sum(row => row.LateEarlyTotalMinutes);
    private string? SourceLockText => Rows.Count == 0
        ? null
        : Rows.Any(row => row.IsLocked)
            ? "Có ngày công đã khóa"
            : "Chưa khóa công & tăng ca";
    private string SourceLockCssClass => Rows.Any(row => row.IsLocked)
        ? "is-locked"
        : "is-unlocked";
    private IReadOnlyList<KhauTruBHXHYTMonthlyWorkdayRow> FilteredRows => Rows
        .Where(row => MatchesSummary(row.DayType, SelectedDayTypeSummaryKey))
        .Where(row => MatchesSummary(row.Status, SelectedStatusSummaryKey))
        .ToArray();
    private IReadOnlyList<SummaryBadge> DayTypeSummaryBadges => BuildBadges(
        Rows.Select(row => row.DayType),
        GetDayTypeShortLabel,
        GetDayTypeSortOrder,
        "Tất cả loại ngày công");
    private IReadOnlyList<SummaryBadge> StatusSummaryBadges => BuildBadges(
        Rows.Select(row => row.Status),
        static key => key,
        static _ => 0,
        "Tất cả trạng thái");
    private string PopupWidth => IsCompactLayout
        ? "calc(100vw - 0.5rem)"
        : "min(90rem, calc(100vw - 1rem))";
    private string PopupHeight => IsCompactLayout
        ? "min(34rem, calc(100vh - 0.5rem))"
        : "min(38rem, calc(100vh - 1rem))";
    private IReadOnlyList<SummaryCard> SummaryCards =>
    [
        new("Ngày thường có chấm công", AttendedRegularWorkdays.ToString("N0", VietnameseCulture), VntaDevExpressIcons.WorkCalendar, ButtonRenderStyle.Primary, "insurance-deduction-monthly-work-summary-card summary-card-primary"),
        new("Tăng ca 1.5", FormatHours(OvertimeMinutes15), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Info, "insurance-deduction-monthly-work-summary-card summary-card-info"),
        new("Tăng ca 2.0", FormatHours(OvertimeMinutes20), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Warning, "insurance-deduction-monthly-work-summary-card summary-card-warning"),
        new("Tăng ca 3.0", FormatHours(OvertimeMinutes30), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Danger, "insurance-deduction-monthly-work-summary-card summary-card-danger"),
        new("Đi trễ/về sớm", LateEarlyMinutesTotal.ToString("N0", VietnameseCulture), VntaDevExpressIcons.SummaryLate, ButtonRenderStyle.Warning, "insurance-deduction-monthly-work-summary-card summary-card-warning")
    ];

    protected override void OnParametersSet()
    {
        if (Visible && !WasVisible)
        {
            SelectedDayTypeSummaryKey = AllSummaryKey;
            SelectedStatusSummaryKey = AllSummaryKey;
            SearchText = null;
        }

        WasVisible = Visible;
    }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    private Task RefreshAsync() => RefreshRequested.InvokeAsync();
    private Task OnCompactLayoutChangedAsync(bool isCompactLayout)
    {
        IsCompactLayout = isCompactLayout;
        return InvokeAsync(StateHasChanged);
    }

    private Task SelectDayTypeSummaryAsync(string key)
    {
        SelectedDayTypeSummaryKey = key;
        return InvokeAsync(StateHasChanged);
    }

    private Task SelectStatusSummaryAsync(string key)
    {
        SelectedStatusSummaryKey = key;
        return InvokeAsync(StateHasChanged);
    }
    private static string FormatHours(int minutes) => (Math.Max(0, minutes) / 60m).ToString("0.##", VietnameseCulture);
    private static string FormatOptionalHours(int minutes) => minutes <= 0 ? string.Empty : FormatHours(minutes);
    private static string FormatOptionalMinutes(int minutes) => minutes <= 0 ? string.Empty : minutes.ToString("N0", VietnameseCulture);
    private static string DisplayOrPlaceholder(string? value) => string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
    private static string GetWeekdayDisplay(DateOnly date)
    {
        var weekday = date.ToDateTime(TimeOnly.MinValue).ToString("dddd", VietnameseCulture);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(weekday);
    }

    private static string GetDayTypeCssClass(KhauTruBHXHYTMonthlyWorkdayRow row) => row.IsRegularWorkday
        ? "monthly-work-day-type"
        : "monthly-work-day-type is-special-day";

    private static string GetShiftCssClass(KhauTruBHXHYTMonthlyWorkdayRow row) =>
        string.Equals(row.ShiftShortName, "--", StringComparison.Ordinal)
            ? "monthly-work-shift is-empty"
            : "monthly-work-shift";

    private static string? GetShiftStyle(KhauTruBHXHYTMonthlyWorkdayRow row) =>
        TryNormalizeHexColor(row.ShiftColorHex, out var color)
            ? $"color: {color};"
            : null;

    private static bool TryNormalizeHexColor(string? value, out string normalized)
    {
        normalized = string.Empty;
        var input = value?.Trim();
        if (input is null
            || input.Length != 7
            || input[0] != '#'
            || !int.TryParse(input.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        normalized = input.ToUpperInvariant();
        return true;
    }

    private static bool MatchesSummary(string? value, string key) =>
        string.IsNullOrEmpty(key)
        || string.Equals(NormalizeKey(value), key, StringComparison.Ordinal);

    private static string NormalizeKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();

    private static int GetDayTypeSortOrder(string key) => key switch
    {
        "Ngày thường" => 0,
        "Ngày nghỉ" => 1,
        "Ngày lễ" => 2,
        _ => 3
    };

    private static string GetDayTypeShortLabel(string key) => key switch
    {
        "Ngày thường" => "Thường",
        "Ngày nghỉ" => "Nghỉ",
        "Ngày lễ" => "Lễ",
        _ => key
    };
    private IReadOnlyList<SummaryBadge> BuildBadges(IEnumerable<string?> values, Func<string, string> shortLabel, Func<string, int> order, string allLabel)
    {
        var badges = new List<SummaryBadge>
        {
            new(AllSummaryKey, "Tất cả", allLabel, Rows.Count)
        };
        badges.AddRange(values
            .GroupBy(NormalizeKey)
            .OrderBy(group => order(group.Key))
            .ThenBy(group => group.Key, StringComparer.CurrentCulture)
            .Select(group => new SummaryBadge(
                group.Key,
                shortLabel(group.Key),
                group.Key == "--" ? "Chưa có trạng thái" : group.Key,
                group.Count())));
        return badges;
    }

    private static string GetDayTypeSummaryCssClass(string key) => key switch
    {
        AllSummaryKey => "insurance-deduction-monthly-work-summary-button summary-all",
        "Ngày nghỉ" or "Ngày lễ" => "insurance-deduction-monthly-work-summary-button summary-special-day",
        _ => "insurance-deduction-monthly-work-summary-button summary-regular-day"
    };

    private static string GetStatusSummaryCssClass(string key) => key switch
    {
        AllSummaryKey => "insurance-deduction-monthly-work-summary-button summary-all",
        "FULL_WORK" or "VR" => "insurance-deduction-monthly-work-summary-button summary-success",
        "LATE_EARLY" or "MISSING_LOG" or "TS" => "insurance-deduction-monthly-work-summary-button summary-warning",
        "ABNORMAL" or "KP" => "insurance-deduction-monthly-work-summary-button summary-danger",
        _ => "insurance-deduction-monthly-work-summary-button summary-neutral"
    };
    private sealed record SummaryCard(string Title, string ValueText, string DevExpressIconUrl, ButtonRenderStyle IconRenderStyle, string CssClass);
    private sealed record SummaryBadge(string Key, string ShortLabel, string Label, int Count);
}
