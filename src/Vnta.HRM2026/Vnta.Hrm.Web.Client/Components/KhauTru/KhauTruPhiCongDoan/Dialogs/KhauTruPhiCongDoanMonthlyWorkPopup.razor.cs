using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruPhiCongDoan;
public partial class KhauTruPhiCongDoanMonthlyWorkPopup
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
    [Parameter] public decimal SalaryWorkDays { get; set; }
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
        .Where(row => MatchesDayTypeSummary(row, SelectedDayTypeSummaryKey))
        .Where(row => MatchesStatusSummary(row, SelectedStatusSummaryKey))
        .ToArray();
    private IReadOnlyList<MonthlyWorkSummaryBadge> DayTypeSummaryBadges => BuildDayTypeSummaryBadges();
    private IReadOnlyList<MonthlyWorkSummaryBadge> StatusSummaryBadges => BuildStatusSummaryBadges();
    private string PopupWidth => IsCompactLayout
        ? "calc(100vw - 0.5rem)"
        : "min(90rem, calc(100vw - 1rem))";
    private string PopupHeight => IsCompactLayout
        ? "min(34rem, calc(100vh - 0.5rem))"
        : "min(38rem, calc(100vh - 1rem))";
    private IReadOnlyList<MonthlyWorkSummaryCard> SummaryCards =>
    [
        new("Công hành chính", FormatWorkday(AdministrativeWorkdays), VntaDevExpressIcons.WorkCalendar, ButtonRenderStyle.Primary, "union-fee-monthly-work-summary-card summary-card-primary"),
        new("Công tính lương", FormatSalaryWorkday(CalculatedSalaryWorkdays), VntaDevExpressIcons.CalculatorCoins, ButtonRenderStyle.Success, "union-fee-monthly-work-summary-card summary-card-success"),
        new("Tăng ca 1.5", FormatHours(OvertimeMinutes15), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Info, "union-fee-monthly-work-summary-card summary-card-info"),
        new("Tăng ca 2.0", FormatHours(OvertimeMinutes20), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Warning, "union-fee-monthly-work-summary-card summary-card-warning"),
        new("Tăng ca 3.0", FormatHours(OvertimeMinutes30), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Danger, "union-fee-monthly-work-summary-card summary-card-danger"),
        new("Tổng giờ tăng ca", FormatHours(TotalOvertimeMinutes), VntaDevExpressIcons.SummaryTotalEmployees, ButtonRenderStyle.Primary, "union-fee-monthly-work-summary-card summary-card-total"),
        new("Đi trễ/về sớm", LateEarlyMinutesTotal.ToString("N0", VietnameseCulture), VntaDevExpressIcons.SummaryLate, ButtonRenderStyle.Warning, "union-fee-monthly-work-summary-card summary-card-warning")
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
    private static string FormatWorkday(decimal value) => value.ToString("0.##", VietnameseCulture);
    private static string FormatSalaryWorkday(decimal value) => value.ToString("0.0", VietnameseCulture);
    private static string FormatHours(int minutes) => (Math.Max(0, minutes) / 60m).ToString("0.##", VietnameseCulture);
    private static string FormatOptionalHours(int minutes) => minutes <= 0 ? string.Empty : FormatHours(minutes);
    private static string FormatOptionalMinutes(int minutes) => minutes <= 0 ? string.Empty : minutes.ToString("N0", VietnameseCulture);
    private static string GetWeekdayDisplay(DateOnly workDate)
    {
        var weekday = workDate.ToDateTime(TimeOnly.MinValue).ToString("dddd", VietnameseCulture);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(weekday);
    }
    private static string GetDayTypeCssClass(MonthlyWorkdayPopupRow row) => row.IsRegularWorkday
        ? "monthly-work-day-type"
        : "monthly-work-day-type is-special-day";
    private static string GetShiftCssClass(MonthlyWorkdayPopupRow row) =>
        string.Equals(row.ShiftShortName, "--", StringComparison.Ordinal)
            ? "monthly-work-shift is-empty"
            : "monthly-work-shift";
    private static string? GetShiftStyle(MonthlyWorkdayPopupRow row) =>
        TryNormalizeHexColor(row.ShiftColorHex, out var color)
            ? $"color: {color};"
            : null;
    private static bool TryNormalizeHexColor(string? value, out string normalizedValue)
    {
        normalizedValue = string.Empty;
        if(string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();
        if(trimmedValue.Length != 7 || trimmedValue[0] != '#'
            || !int.TryParse(trimmedValue.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        normalizedValue = trimmedValue.ToUpperInvariant();
        return true;
    }
    private IReadOnlyList<MonthlyWorkSummaryBadge> BuildDayTypeSummaryBadges()
    {
        var badges = new List<MonthlyWorkSummaryBadge>
        {
            new(AllSummaryKey, "Tất cả", "Tất cả loại ngày công", Rows.Count)
        };

        badges.AddRange(Rows
            .GroupBy(row => NormalizeSummaryKey(row.DayType))
            .OrderBy(group => GetDayTypeSortOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.CurrentCulture)
            .Select(group => new MonthlyWorkSummaryBadge(
                group.Key,
                GetDayTypeShortLabel(group.Key),
                group.Key,
                group.Count())));

        return badges;
    }
    private IReadOnlyList<MonthlyWorkSummaryBadge> BuildStatusSummaryBadges()
    {
        var badges = new List<MonthlyWorkSummaryBadge>
        {
            new(AllSummaryKey, "Tất cả", "Tất cả trạng thái", Rows.Count)
        };

        badges.AddRange(Rows
            .GroupBy(row => NormalizeSummaryKey(row.Status))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new MonthlyWorkSummaryBadge(
                group.Key,
                group.Key,
                string.Equals(group.Key, "--", StringComparison.Ordinal) ? "Chưa có trạng thái" : group.Key,
                group.Count())));

        return badges;
    }
    private static bool MatchesDayTypeSummary(MonthlyWorkdayPopupRow row, string key) =>
        string.IsNullOrEmpty(key) || string.Equals(NormalizeSummaryKey(row.DayType), key, StringComparison.Ordinal);
    private static bool MatchesStatusSummary(MonthlyWorkdayPopupRow row, string key) =>
        string.IsNullOrEmpty(key) || string.Equals(NormalizeSummaryKey(row.Status), key, StringComparison.Ordinal);
    private static string NormalizeSummaryKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
    private static int GetDayTypeSortOrder(string dayType) => dayType switch
    {
        "Ngày thường" => 0,
        "Ngày nghỉ" => 1,
        "Ngày lễ" => 2,
        _ => 3
    };
    private static string GetDayTypeShortLabel(string dayType) => dayType switch
    {
        "Ngày thường" => "Thường",
        "Ngày nghỉ" => "Nghỉ",
        "Ngày lễ" => "Lễ",
        _ => dayType
    };
    private static string GetDayTypeSummaryCssClass(string key) => key switch
    {
        AllSummaryKey => "union-fee-monthly-work-summary-button summary-all",
        "Ngày nghỉ" or "Ngày lễ" => "union-fee-monthly-work-summary-button summary-special-day",
        _ => "union-fee-monthly-work-summary-button summary-regular-day"
    };
    private static string GetStatusSummaryCssClass(string key) => key switch
    {
        AllSummaryKey => "union-fee-monthly-work-summary-button summary-all",
        "FULL_WORK" or "VR" => "union-fee-monthly-work-summary-button summary-success",
        "LATE_EARLY" or "MISSING_LOG" or "TS" => "union-fee-monthly-work-summary-button summary-warning",
        "ABNORMAL" or "KP" => "union-fee-monthly-work-summary-button summary-danger",
        _ => "union-fee-monthly-work-summary-button summary-neutral"
    };
    private sealed record MonthlyWorkSummaryCard(
        string Title,
        string ValueText,
        string DevExpressIconUrl,
        ButtonRenderStyle IconRenderStyle,
        string CssClass);
    private sealed record MonthlyWorkSummaryBadge(
        string Key,
        string ShortLabel,
        string Label,
        int Count);
}
