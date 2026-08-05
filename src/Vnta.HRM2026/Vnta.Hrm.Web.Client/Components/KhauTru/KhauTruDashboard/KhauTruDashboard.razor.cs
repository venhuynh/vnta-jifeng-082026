using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruTongHop;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruDashboard;

public partial class KhauTruDashboard : IDisposable
{
    private const int MinimumSupportedYear = 2026;
    private const int MinimumSupportedMonth = 6;
    private const int MaximumSupportedYear = 2100;
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly IReadOnlyList<MonthOption> MonthOptions = Enumerable.Range(1, 12)
        .Select(month => new MonthOption(month, $"Tháng {month:00}"))
        .ToArray();

    private readonly CancellationTokenSource disposalTokenSource = new();

    [Inject] private PayrollDeductionDashboardDataProvider DataProvider { get; set; } = default!;
    [Inject] private ILogger<KhauTruDashboard> Logger { get; set; } = default!;

    private PayrollDeductionDashboardDto? Dashboard { get; set; }
    private string? LoadErrorMessage { get; set; }
    private bool IsLoading { get; set; }
    private bool IsMediumScreen { get; set; }
    private bool IsLargeScreen { get; set; }
    private int ToolbarMonth { get; set; } = GetDefaultPeriod().Month;
    private int ToolbarYear { get; set; } = GetDefaultPeriod().Year;

    private IReadOnlyList<MonthOption> AvailableMonths => ToolbarYear == MinimumSupportedYear
        ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
        : MonthOptions;
    private bool CanChangePeriod => !IsLoading;
    private string PeriodLabel => FormatPeriod(ToolbarMonth, ToolbarYear);
    private decimal AverageDeduction => Dashboard?.Overview.TotalCount > 0
        ? Dashboard.Overview.TotalDeductionAmount / Dashboard.Overview.TotalCount
        : 0m;
    private double DashboardLockRate => Dashboard?.Overview.TotalCount > 0
        ? (double)Dashboard.Overview.LockedCount / Dashboard.Overview.TotalCount
        : 0d;
    private string DashboardLockRateDisplay => DashboardLockRate.ToString("P0", DisplayCulture);

    protected override Task OnInitializedAsync() => LoadDashboardAsync();

    private async Task LoadDashboardAsync()
    {
        if(IsLoading || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsLoading = true;
        LoadErrorMessage = null;
        try
        {
            Dashboard = await DataProvider.GetDashboardAsync(
                ToolbarMonth,
                ToolbarYear,
                disposalTokenSource.Token);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception exception)
        {
            Dashboard = null;
            LoadErrorMessage = "Có lỗi khi tải dữ liệu dashboard khấu trừ. Vui lòng thử lại.";
            Logger.LogError(
                exception,
                "Không thể tải dashboard khấu trừ cho kỳ {PayrollMonth}/{PayrollYear}.",
                ToolbarMonth,
                ToolbarYear);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnYearChangedAsync(int year)
    {
        ToolbarYear = year;
        if(ToolbarYear == MinimumSupportedYear && ToolbarMonth < MinimumSupportedMonth)
        {
            ToolbarMonth = MinimumSupportedMonth;
        }

        await LoadDashboardAsync();
    }

    private async Task OnMonthChangedAsync(int month)
    {
        ToolbarMonth = month;
        await LoadDashboardAsync();
    }

    private void OnSmallScreenChanged(bool isSmallScreen)
    {
        if(isSmallScreen)
        {
            IsMediumScreen = false;
            IsLargeScreen = false;
        }
    }

    private string GetMoneyChangeText()
    {
        var previousAmount = Dashboard?.PreviousPeriodOverview.TotalDeductionAmount ?? 0m;
        return previousAmount == 0m
            ? "So với kỳ trước: chưa có dữ liệu"
            : $"So với kỳ trước: {GetMoneyChangeRatio():P1}";
    }

    private double GetMoneyChangeRatio() => GetChangeRatio(
        Dashboard?.Overview.TotalDeductionAmount ?? 0m,
        Dashboard?.PreviousPeriodOverview.TotalDeductionAmount ?? 0m);

    private string GetCountChangeText()
    {
        var previousCount = Dashboard?.PreviousPeriodOverview.TotalCount ?? 0;
        return previousCount == 0
            ? "So với kỳ trước: chưa có dữ liệu"
            : $"So với kỳ trước: {GetCountChangeRatio():P1}";
    }

    private double GetCountChangeRatio() => GetChangeRatio(
        Dashboard?.Overview.TotalCount ?? 0,
        Dashboard?.PreviousPeriodOverview.TotalCount ?? 0);

    private static double GetChangeRatio(decimal current, decimal previous) =>
        previous == 0m ? 0d : (double)((current - previous) / previous);

    private static double GetChangeRatio(int current, int previous) =>
        previous == 0 ? 0d : (double)(current - previous) / previous;

    private static string FormatMoney(decimal amount) => amount.ToString("N0", DisplayCulture) + " đ";

    private static string FormatPeriod(int month, int year) => $"{month:00}/{year}";

    private static (int Month, int Year) GetDefaultPeriod()
    {
        var now = DateTime.UtcNow.AddHours(7);
        return now.Year == MinimumSupportedYear && now.Month < MinimumSupportedMonth
            ? (MinimumSupportedMonth, MinimumSupportedYear)
            : (now.Month, Math.Clamp(now.Year, MinimumSupportedYear, MaximumSupportedYear));
    }

    public void Dispose() => disposalTokenSource.Dispose();

    private sealed record MonthOption(int Value, string Text);
}
