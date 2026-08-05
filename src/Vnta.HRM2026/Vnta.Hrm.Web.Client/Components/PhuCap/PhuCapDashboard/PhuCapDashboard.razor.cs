using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.PhuCap.PhuCapDashboard.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDashboard.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapDashboard;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDashboard;

public partial class PhuCapDashboard : IDisposable
{
    private static readonly IReadOnlyList<PayrollAllowanceDashboardMonthOption> MonthOptions = Enumerable.Range(1, 12)
        .Select(month => new PayrollAllowanceDashboardMonthOption(month, $"Tháng {month:00}"))
        .ToArray();
    private readonly CancellationTokenSource disposalTokenSource = new();

    [Inject] private PayrollAllowanceDashboardDataProvider DataProvider { get; set; } = default!;
    [Inject] private ILogger<PhuCapDashboard> Logger { get; set; } = default!;
    private PayrollAllowanceDashboardDto? Dashboard { get; set; }
    private string? LoadErrorMessage { get; set; }
    private bool IsLoading { get; set; }
    private bool IsMediumScreen { get; set; }
    private bool IsLargeScreen { get; set; }
    private int ToolbarMonth { get; set; } = GetDefaultPeriod().Month;
    private int ToolbarYear { get; set; } = GetDefaultPeriod().Year;
    private IReadOnlyList<PayrollAllowanceDashboardMonthOption> AvailableMonths => ToolbarYear == PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedYear
        ? MonthOptions.Where(option => option.Value >= PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedMonth).ToArray() : MonthOptions;
    private bool CanChangePeriod => !IsLoading;
    private string PeriodLabel => $"{ToolbarMonth:00}/{ToolbarYear}";

    protected override Task OnInitializedAsync() => LoadDashboardAsync();

    private async Task LoadDashboardAsync()
    {
        if (IsLoading || disposalTokenSource.IsCancellationRequested) return;
        IsLoading = true;
        LoadErrorMessage = null;
        try { Dashboard = await DataProvider.GetDashboardAsync(ToolbarMonth, ToolbarYear, disposalTokenSource.Token); }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Dashboard = null;
            LoadErrorMessage = "Có lỗi khi tải dữ liệu dashboard. Vui lòng thử lại.";
            Logger.LogError(exception, "Không thể tải dashboard phụ cấp cho kỳ {PayrollMonth}/{PayrollYear}.", ToolbarMonth, ToolbarYear);
        }
        finally { IsLoading = false; }
    }

    private async Task OnYearChangedAsync(int year)
    {
        ToolbarYear = year;
        if (ToolbarYear == PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedYear
            && ToolbarMonth < PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedMonth)
        {
            ToolbarMonth = PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedMonth;
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
        if (!isSmallScreen) return;
        IsMediumScreen = false;
        IsLargeScreen = false;
    }

    private static (int Month, int Year) GetDefaultPeriod()
    {
        var now = DateTime.UtcNow.AddHours(7);
        return now.Year == PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedYear
               && now.Month < PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedMonth
            ? (PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedMonth, PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedYear)
            : (now.Month, Math.Clamp(
                now.Year,
                PayrollAllowanceDashboardPeriodPolicy.MinimumSupportedYear,
                PayrollAllowanceDashboardPeriodPolicy.MaximumSupportedYear));
    }

    public void Dispose() => disposalTokenSource.Dispose();
}
