namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Đại diện kiểu <c>MonthOption</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public sealed record MonthOption(int Value, string Text);

/// <summary>Đại diện kiểu <c>ResponsibilitySummaryBadge</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public sealed record ResponsibilitySummaryBadge(string Key, string Label, string ShortLabel, int Count);

/// <summary>Thực hiện xử lý cho luồng <c>ResponsibilityAllowancePeriodKey</c>.</summary>
public readonly record struct ResponsibilityAllowancePeriodKey(int Year, int Month)
{
    /// <summary>Lấy cho luồng <c>GetPreviousPeriod</c>.</summary>
    public ResponsibilityAllowancePeriodKey GetPreviousPeriod() =>
        Month == 1
            ? new ResponsibilityAllowancePeriodKey(Year - 1, 12)
            : new ResponsibilityAllowancePeriodKey(Year, Month - 1);
}
