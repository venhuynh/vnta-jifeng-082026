using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;

namespace Vnta.Hrm.Application.PhuCap.PhuCapDashboard.Policies;

public readonly record struct PayrollAllowanceDashboardChangeResult(double Ratio, bool HasPreviousData);
public readonly record struct PayrollAllowanceDashboardKpiResult(decimal AverageAllowance, double LockRate);

public static class PayrollAllowanceDashboardMetricPolicy
{
    public static PayrollAllowanceDashboardChangeResult Compare(decimal current, decimal previous) =>
        previous == 0m ? new(0d, false) : new((double)((current - previous) / previous), true);

    public static PayrollAllowanceDashboardChangeResult Compare(int current, int previous) =>
        previous == 0 ? new(0d, false) : new((double)(current - previous) / previous, true);

    public static PayrollAllowanceDashboardKpiResult CalculateKpis(PayrollAllowanceDashboardOverviewDto overview) =>
        overview.TotalCount <= 0 ? new(0m, 0d) : new(overview.TotalAllowanceAmount / overview.TotalCount, (double)overview.LockedCount / overview.TotalCount);
}

public static class PayrollAllowanceDashboardPeriodPolicy
{
    public const int MinimumSupportedYear = 2026;
    public const int MinimumSupportedMonth = 6;
    public const int MaximumSupportedYear = 2100;

    public static void Validate(PayrollAllowanceDashboardFilter filter)
    {
        if (filter.HistoryMonthCount is < 2 or > 12) throw new InvalidOperationException("Số kỳ lịch sử của dashboard phải nằm trong khoảng 2 đến 12.");
        if (filter.DepartmentTake is < 1 or > 20) throw new InvalidOperationException("Số phòng ban hiển thị phải nằm trong khoảng 1 đến 20.");
        if (filter.PayrollYear < MinimumSupportedYear || filter.PayrollYear > MaximumSupportedYear) throw new InvalidOperationException($"Năm dữ liệu phải nằm trong khoảng {MinimumSupportedYear} đến {MaximumSupportedYear}.");
        if (filter.PayrollMonth is < 1 or > 12) throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
        if (filter.PayrollYear == MinimumSupportedYear && filter.PayrollMonth < MinimumSupportedMonth) throw new InvalidOperationException($"Mốc dữ liệu tổng hợp phụ cấp bắt đầu từ {MinimumSupportedMonth:00}/{MinimumSupportedYear}.");
    }
}
