using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDashboard;

public partial class PhuCapDashboardBreakdownReport
{
    [Parameter, EditorRequired] public IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto> Breakdown { get; set; } = [];
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public EventCallback RefreshRequested { get; set; }

    private IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto> NonZeroBreakdown =>
        Breakdown.Where(item => item.Amount != 0m).ToArray();

    private IReadOnlyList<BreakdownChartPoint> ChartPoints => NonZeroBreakdown
        .GroupBy(item => item.AllowanceType)
        .Select(group => new
        {
            AllowanceType = group.Key,
            Amount = group.Sum(item => item.Amount)
        })
        .Select(item => new BreakdownChartPoint(
            item.AllowanceType,
            (double)item.Amount,
            FormatLabel(item.Amount, TotalBreakdownAmount)))
        .ToArray();

    private decimal TotalBreakdownAmount => NonZeroBreakdown.Sum(item => item.Amount);

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    private static void CustomizeBreakdownPoint(ChartSeriesPointCustomizationSettings pointSettings)
    {
        var point = pointSettings.Point.DataItems.OfType<BreakdownChartPoint>().FirstOrDefault();
        if(point is null) return;

        pointSettings.PointLabel.Texts = [point.Label];
    }

    private static string FormatLabel(decimal amount, decimal totalAmount) =>
        $"{FormatAmountLabel(amount)} ({FormatPercent(amount, totalAmount)})";

    private static string FormatAmountLabel(decimal amount)
    {
        var compactAmount = Math.Truncate(amount / 1_000m);
        return compactAmount.ToString("N0", DisplayCulture) + "tr đ";
    }

    private static string FormatPercent(decimal amount, decimal totalAmount) =>
        totalAmount > 0m
            ? (amount / totalAmount).ToString("P1", DisplayCulture)
            : 0m.ToString("P1", DisplayCulture);

    private sealed record BreakdownChartPoint(string AllowanceType, double Amount, string Label);
}
