using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDashboard;

public partial class PhuCapDashboardTrendReport
{
    [Parameter, EditorRequired] public IReadOnlyList<PayrollAllowanceDashboardTrendPointDto> Trend { get; set; } = [];
    [Parameter, EditorRequired] public int PayrollMonth { get; set; }
    [Parameter, EditorRequired] public int PayrollYear { get; set; }
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public EventCallback RefreshRequested { get; set; }

    private string PeriodLabel => Trend.Count > 0
        ? FormatPeriod(Trend[^1].PayrollMonth, Trend[^1].PayrollYear)
        : FormatPeriod(PayrollMonth, PayrollYear);

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly ChartElementFormat MoneyLabelFormat = ChartElementFormat.FromLdmlString("#,##0");

    private static string FormatPeriod(int month, int year) => $"{month:00}/{year}";

    private static string FormatMoney(decimal amount) => amount.ToString("N0", DisplayCulture) + " đ";

    private static string FormatMoney(double amount) => amount.ToString("N0", DisplayCulture) + " đ";
}
