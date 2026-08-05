using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;
using Vnta.Hrm.Web.Client.Services.DataProviders;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.DashboardBangChamCong;

public partial class BangChamCongDashboard : IDisposable
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly IReadOnlyList<MonthOption> MonthOptions = Enumerable.Range(1, 12).Select(month => new MonthOption(month, $"Tháng {month:00}")).ToArray();
    private readonly CancellationTokenSource disposalTokenSource = new();

    [Inject] private AttendanceTimesheetDashboardDataProvider DataProvider { get; set; } = default!;
    [Inject] private ILogger<BangChamCongDashboard> Logger { get; set; } = default!;

    private AttendanceTimesheetDashboardDto? Dashboard { get; set; }
    private string? LoadErrorMessage { get; set; }
    private bool IsLoading { get; set; }
    private int ToolbarMonth { get; set; } = DateTime.Today.Month;
    private int ToolbarYear { get; set; } = DateTime.Today.Year;

    private bool CanChangePeriod => !IsLoading;
    private string PeriodLabel => $"{ToolbarMonth:00}/{ToolbarYear}";

    protected override Task OnInitializedAsync() => LoadDashboardAsync();

    private async Task LoadDashboardAsync()
    {
        if(IsLoading || disposalTokenSource.IsCancellationRequested) return;

        IsLoading = true;
        LoadErrorMessage = null;
        try
        {
            Dashboard = await DataProvider.GetDashboardAsync(ToolbarMonth, ToolbarYear, disposalTokenSource.Token);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(Exception exception)
        {
            Dashboard = null;
            LoadErrorMessage = "Có lỗi khi tải dữ liệu tổng quan bảng công. Vui lòng thử lại.";
            Logger.LogError(exception, "Không thể tải Dashboard bảng công cho kỳ {WorkMonth}/{WorkYear}.", ToolbarMonth, ToolbarYear);
        }
        finally { IsLoading = false; }
    }

    private async Task OnYearChangedAsync(int year) { ToolbarYear = year; await LoadDashboardAsync(); }
    private async Task OnMonthChangedAsync(int month) { ToolbarMonth = month; await LoadDashboardAsync(); }
    private static string FormatHours(int minutes) => $"{Math.Max(0, minutes) / 60:N0} giờ {Math.Max(0, minutes) % 60:00} phút";
    private static string FormatMinutes(int minutes) => $"{Math.Max(0, minutes):N0} phút";
    public void Dispose() => disposalTokenSource.Dispose();
    private sealed record MonthOption(int Value, string Text);
}
