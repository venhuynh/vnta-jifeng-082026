using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.MonthlyWork;

/// <summary>Popup dùng chung để đối chiếu bảng công tháng theo ngữ cảnh chấm công.</summary>
public partial class ChamCongMonthlyWorkPopup
{
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp thâm niên.</summary>
    private const string AllSummaryKey = "";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp thâm niên.</summary>
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp thâm niên.</summary>
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

    /// <summary>Giá trị <c>SelectedDayTypeSummaryKey</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private string SelectedDayTypeSummaryKey { get; set; } = AllSummaryKey;
    /// <summary>Giá trị <c>SelectedStatusSummaryKey</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private string SelectedStatusSummaryKey { get; set; } = AllSummaryKey;
    /// <summary>Giá trị <c>SearchText</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private string? SearchText { get; set; }
    /// <summary>Giá trị <c>WasVisible</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private bool WasVisible { get; set; }
    /// <summary>Giá trị <c>IsCompactLayout</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private bool IsCompactLayout { get; set; }
    /// <summary>Giá trị <c>AdministrativeWorkdays</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private decimal AdministrativeWorkdays => Rows.Count(row => row.IsRegularWorkday && row.HasCheckInOrOut);
    /// <summary>Giá trị <c>CalculatedSalaryWorkdays</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private decimal CalculatedSalaryWorkdays => AdministrativeWorkdays - (LateEarlyMinutesTotal / 480m);
    /// <summary>Giá trị <c>OvertimeMinutes15</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private int OvertimeMinutes15 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes15));
    /// <summary>Giá trị <c>OvertimeMinutes20</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private int OvertimeMinutes20 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes20));
    /// <summary>Giá trị <c>OvertimeMinutes30</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private int OvertimeMinutes30 => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes30));
    /// <summary>Giá trị <c>TotalOvertimeMinutes</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private int TotalOvertimeMinutes => Rows.Sum(row => Math.Max(0, row.OvertimeMinutes));
    /// <summary>Giá trị <c>LateEarlyMinutesTotal</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private int LateEarlyMinutesTotal => Rows.Sum(row => row.LateEarlyTotalMinutes);
    /// <summary>Giá trị <c>SourceLockText</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private string SourceLockText => Rows.Any(row => row.IsLocked) ? "Có ngày công đã khóa" : "Chưa khóa công & tăng ca";
    /// <summary>Giá trị <c>SourceLockCssClass</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private string SourceLockCssClass => Rows.Any(row => row.IsLocked) ? "is-locked" : "is-unlocked";
    /// <summary>Giá trị <c>FilteredRows</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private IReadOnlyList<MonthlyWorkdayPopupRow> FilteredRows => Rows
        .Where(row => MatchesDayTypeSummary(row, SelectedDayTypeSummaryKey))
        .Where(row => MatchesStatusSummary(row, SelectedStatusSummaryKey))
        .ToArray();
    /// <summary>Giá trị <c>DayTypeSummaryBadges</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private IReadOnlyList<MonthlyWorkSummaryBadge> DayTypeSummaryBadges => BuildDayTypeSummaryBadges();
    /// <summary>Giá trị <c>StatusSummaryBadges</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private IReadOnlyList<MonthlyWorkSummaryBadge> StatusSummaryBadges => BuildStatusSummaryBadges();
    /// <summary>Giá trị <c>PopupWidth</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private string PopupWidth => IsCompactLayout
        ? "calc(100vw - 0.5rem)"
        : "min(90rem, calc(100vw - 1rem))";
    /// <summary>Giá trị <c>PopupHeight</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private string PopupHeight => IsCompactLayout
        ? "min(34rem, calc(100vh - 0.5rem))"
        : "min(38rem, calc(100vh - 1rem))";
    /// <summary>Giá trị <c>SummaryCards</c> được sử dụng bởi giao diện phụ cấp thâm niên.</summary>
    private IReadOnlyList<MonthlyWorkSummaryCard> SummaryCards =>
    [
        new("Công hành chính", FormatWorkday(AdministrativeWorkdays), VntaDevExpressIcons.WorkCalendar, ButtonRenderStyle.Primary, "seniority-monthly-work-summary-card summary-card-primary"),
        new("Công tính lương", FormatSalaryWorkday(CalculatedSalaryWorkdays), VntaDevExpressIcons.CalculatorCoins, ButtonRenderStyle.Success, "seniority-monthly-work-summary-card summary-card-success"),
        new("Tăng ca 1.5", FormatHours(OvertimeMinutes15), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Info, "seniority-monthly-work-summary-card summary-card-info"),
        new("Tăng ca 2.0", FormatHours(OvertimeMinutes20), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Warning, "seniority-monthly-work-summary-card summary-card-warning"),
        new("Tăng ca 3.0", FormatHours(OvertimeMinutes30), VntaDevExpressIcons.Attendance, ButtonRenderStyle.Danger, "seniority-monthly-work-summary-card summary-card-danger"),
        new("Tổng giờ tăng ca", FormatHours(TotalOvertimeMinutes), VntaDevExpressIcons.SummaryTotalEmployees, ButtonRenderStyle.Primary, "seniority-monthly-work-summary-card summary-card-total"),
        new("Đi trễ/về sớm", LateEarlyMinutesTotal.ToString("N0", VietnameseCulture), VntaDevExpressIcons.SummaryLate, ButtonRenderStyle.Warning, "seniority-monthly-work-summary-card summary-card-warning")
    ];

    /// <summary>Xử lý sự kiện cho luồng <c>OnParametersSet</c>.</summary>
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

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    /// <summary>Làm mới cho luồng <c>RefreshAsync</c>.</summary>
    private Task RefreshAsync() => RefreshRequested.InvokeAsync();

    /// <summary>Xử lý sự kiện cho luồng <c>OnCompactLayoutChangedAsync</c>.</summary>
    private Task OnCompactLayoutChangedAsync(bool isCompactLayout)
    {
        IsCompactLayout = isCompactLayout;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Cập nhật lựa chọn cho luồng <c>SelectDayTypeSummaryAsync</c>.</summary>
    private Task SelectDayTypeSummaryAsync(string key)
    {
        SelectedDayTypeSummaryKey = key;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Cập nhật lựa chọn cho luồng <c>SelectStatusSummaryAsync</c>.</summary>
    private Task SelectStatusSummaryAsync(string key)
    {
        SelectedStatusSummaryKey = key;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Định dạng cho luồng <c>FormatWorkday</c>.</summary>
    private static string FormatWorkday(decimal value) => value.ToString("0.##", VietnameseCulture);

    /// <summary>Định dạng cho luồng <c>FormatSalaryWorkday</c>.</summary>
    private static string FormatSalaryWorkday(decimal value) => value.ToString("0.0", VietnameseCulture);

    /// <summary>Định dạng cho luồng <c>FormatHours</c>.</summary>
    private static string FormatHours(int minutes) => (Math.Max(0, minutes) / 60m).ToString("0.##", VietnameseCulture);

    /// <summary>Định dạng cho luồng <c>FormatOptionalHours</c>.</summary>
    private static string FormatOptionalHours(int minutes) => minutes <= 0 ? string.Empty : FormatHours(minutes);

    /// <summary>Định dạng cho luồng <c>FormatOptionalMinutes</c>.</summary>
    private static string FormatOptionalMinutes(int minutes) => minutes <= 0 ? string.Empty : minutes.ToString("N0", VietnameseCulture);

    /// <summary>Lấy cho luồng <c>GetWeekdayDisplay</c>.</summary>
    private static string GetWeekdayDisplay(DateOnly workDate)
    {
        var weekday = workDate.ToDateTime(TimeOnly.MinValue).ToString("dddd", VietnameseCulture);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(weekday);
    }

    /// <summary>Lấy cho luồng <c>GetDayTypeCssClass</c>.</summary>
    private static string GetDayTypeCssClass(MonthlyWorkdayPopupRow row) => row.IsRegularWorkday
        ? "monthly-work-day-type"
        : "monthly-work-day-type is-special-day";

    /// <summary>Lấy cho luồng <c>GetShiftCssClass</c>.</summary>
    private static string GetShiftCssClass(MonthlyWorkdayPopupRow row) =>
        string.Equals(row.ShiftShortName, "--", StringComparison.Ordinal)
            ? "monthly-work-shift is-empty"
            : "monthly-work-shift";

    /// <summary>Lấy cho luồng <c>GetShiftStyle</c>.</summary>
    private static string? GetShiftStyle(MonthlyWorkdayPopupRow row) =>
        TryNormalizeHexColor(row.ShiftColorHex, out var color)
            ? $"color: {color};"
            : null;

    /// <summary>Thực hiện xử lý cho luồng <c>TryNormalizeHexColor</c>.</summary>
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

    /// <summary>Tạo cho luồng <c>BuildDayTypeSummaryBadges</c>.</summary>
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

    /// <summary>Tạo cho luồng <c>BuildStatusSummaryBadges</c>.</summary>
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

    /// <summary>Thực hiện xử lý cho luồng <c>MatchesDayTypeSummary</c>.</summary>
    private static bool MatchesDayTypeSummary(MonthlyWorkdayPopupRow row, string key) =>
        string.IsNullOrEmpty(key) || string.Equals(NormalizeSummaryKey(row.DayType), key, StringComparison.Ordinal);

    /// <summary>Thực hiện xử lý cho luồng <c>MatchesStatusSummary</c>.</summary>
    private static bool MatchesStatusSummary(MonthlyWorkdayPopupRow row, string key) =>
        string.IsNullOrEmpty(key) || string.Equals(NormalizeSummaryKey(row.Status), key, StringComparison.Ordinal);

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeSummaryKey</c>.</summary>
    private static string NormalizeSummaryKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();

    /// <summary>Lấy cho luồng <c>GetDayTypeSortOrder</c>.</summary>
    private static int GetDayTypeSortOrder(string dayType) => dayType switch
    {
        "Ngày thường" => 0,
        "Ngày nghỉ" => 1,
        "Ngày lễ" => 2,
        _ => 3
    };

    /// <summary>Lấy cho luồng <c>GetDayTypeShortLabel</c>.</summary>
    private static string GetDayTypeShortLabel(string dayType) => dayType switch
    {
        "Ngày thường" => "Thường",
        "Ngày nghỉ" => "Nghỉ",
        "Ngày lễ" => "Lễ",
        _ => dayType
    };

    /// <summary>Lấy cho luồng <c>GetDayTypeSummaryCssClass</c>.</summary>
    private static string GetDayTypeSummaryCssClass(string key) => key switch
    {
        AllSummaryKey => "seniority-monthly-work-summary-button summary-all",
        "Ngày nghỉ" or "Ngày lễ" => "seniority-monthly-work-summary-button summary-special-day",
        _ => "seniority-monthly-work-summary-button summary-regular-day"
    };

    /// <summary>Lấy cho luồng <c>GetStatusSummaryCssClass</c>.</summary>
    private static string GetStatusSummaryCssClass(string key) => key switch
    {
        AllSummaryKey => "seniority-monthly-work-summary-button summary-all",
        "FULL_WORK" or "VR" => "seniority-monthly-work-summary-button summary-success",
        "LATE_EARLY" or "MISSING_LOG" or "TS" => "seniority-monthly-work-summary-button summary-warning",
        "ABNORMAL" or "KP" => "seniority-monthly-work-summary-button summary-danger",
        _ => "seniority-monthly-work-summary-button summary-neutral"
    };

    /// <summary>Đại diện kiểu <c>MonthlyWorkSummaryCard</c> phục vụ giao diện phụ cấp thâm niên.</summary>
    private sealed record MonthlyWorkSummaryCard(
        string Title,
        string ValueText,
        string DevExpressIconUrl,
        ButtonRenderStyle IconRenderStyle,
        string CssClass);

    /// <summary>Đại diện kiểu <c>MonthlyWorkSummaryBadge</c> phục vụ giao diện phụ cấp thâm niên.</summary>
    private sealed record MonthlyWorkSummaryBadge(
        string Key,
        string ShortLabel,
        string Label,
        int Count);
}
