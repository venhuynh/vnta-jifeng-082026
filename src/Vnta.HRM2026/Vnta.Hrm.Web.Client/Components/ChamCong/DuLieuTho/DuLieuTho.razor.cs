using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.DuLieuTho;

public partial class DuLieuTho : IDisposable
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly CancellationTokenSource disposalTokenSource = new();
    private const int SearchResultLimit = 2000;

    [Inject]
    private IAttendanceDailySummaryReadService AttendanceDailySummaryReadService { get; set; } = default!;

    [Inject]
    private IAttendanceDailySummaryService AttendanceDailySummaryService { get; set; } = default!;

    [Inject]
    private AttendanceDeviceDataProvider AttendanceDeviceDataProvider { get; set; } = default!;

    [Inject]
    private IAdmsDeviceCommandService DeviceCommandService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    private IReadOnlyList<AttendanceDailySummaryRecord> Summaries { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private DateTime? ToolbarDate { get; set; } = DateTime.Today;
    private bool IsLoading { get; set; }
    private bool IsRebuildingDailySummaries { get; set; }
    private bool IsCreatingAttendanceLogQueryCommand { get; set; }
    private bool IsDetailPopupVisible { get; set; }
    private AttendanceDailySummaryRecord? DetailSummary { get; set; }

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

    private bool CanInteract => !IsLoading && !HasLoadError;

    private bool CanCreateAttendanceLogQueryCommand => CanInteract && !IsCreatingAttendanceLogQueryCommand;

    private bool CanResetFilters =>
        !ToolbarDate.HasValue
        || ToolbarDate.Value.Date != DateTime.Today;

    private bool CanExport => !IsLoading && Summaries.Count > 0;

    private bool CanExportSelected => CanExport && GetSelectedSummaries().Count > 0;

    private string EmptyStateTitle => CanResetFilters
        ? "Không tìm thấy dữ liệu tổng kết phù hợp"
        : "Chưa có dữ liệu tổng kết ngày";

    private string EmptyStateMessage => CanResetFilters
        ? "Hãy nới điều kiện lọc hoặc xóa bộ lọc để xem thêm dữ liệu."
        : "Bảng attendance_daily_summaries sẽ hiển thị tại đây sau khi có dữ liệu tổng kết.";

    private string LoadingPanelText => IsRebuildingDailySummaries
        ? "Đang tổng hợp dữ liệu chấm công..."
        : "Đang tải dữ liệu attendance_daily_summaries...";

    private string EmployeeCountSummaryDisplayText =>
        $"Nhân viên đang hiển thị: {{0:N0}} / {Summaries.Count:N0}";

    private AttendanceDailySummaryRecord? SelectedSummary =>
        GetSelectedSummaries().Count == 1 ? GetSelectedSummaries()[0] : null;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
        await base.OnInitializedAsync();
    }

    private async Task ReloadAsync()
    {
        if(disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        await LoadSummariesAsync(showLoading: true);
    }

    private async Task LoadSummariesAsync(bool showLoading)
    {
        LoadErrorMessage = null;

        if(showLoading)
        {
            IsLoading = true;
        }

        try
        {
            await ClearSelectionAsync();
            var rows = await AttendanceDailySummaryReadService.SearchAsync(BuildFilter(), disposalTokenSource.Token);
            Summaries = rows.Select(MapRecord).ToList();
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            Summaries = [];
            LoadErrorMessage = "Có lỗi khi tải dữ liệu tổng kết chấm công. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải dữ liệu attendance_daily_summaries.");
        }
        finally
        {
            if(showLoading)
            {
                IsLoading = false;
            }
        }
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;

        return Task.CompletedTask;
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private async Task ViewAttendanceSummariesAsync()
    {
        await ReloadAsync();
    }

    private async Task CreateAttendanceLogQueryCommandsAsync()
    {
        if(disposalTokenSource.IsCancellationRequested || IsCreatingAttendanceLogQueryCommand)
        {
            return;
        }

        var attDate = (ToolbarDate ?? DateTime.Today).Date;
        var devices = await GetActiveAttendanceDevicesWithSerialAsync();

        if(devices.Count == 0)
        {
            ToastService.ShowWarning("Chưa có máy chấm công đang dùng và có số serial để tạo lệnh tải chấm công.");
            return;
        }

        IsCreatingAttendanceLogQueryCommand = true;

        try
        {
            foreach(var device in devices)
            {
                var serialNumber = device.SerialNumber;
                if(string.IsNullOrWhiteSpace(serialNumber))
                {
                    continue;
                }

                await DeviceCommandService.CreateAsync(
                    new UpsertAdmsDeviceCommandRequest(
                        serialNumber,
                        BuildAttendanceLogQueryCommandContent(attDate),
                        DateTime.Now,
                        $"Query AttLog Date={attDate:yyyy-MM-dd}"),
                    disposalTokenSource.Token);
            }

            ToastService.ShowSuccess(
                devices.Count == 1
                    ? $"Đã tạo lệnh tải chấm công ngày {attDate:dd/MM/yyyy} cho máy chấm công."
                    : $"Đã tạo lệnh tải chấm công ngày {attDate:dd/MM/yyyy} cho {devices.Count:N0} máy chấm công.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể tạo lệnh tải dữ liệu chấm công.");
        }
        finally
        {
            IsCreatingAttendanceLogQueryCommand = false;
        }
    }

    private async Task RebuildDailySummaryAsync()
    {
        if(disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        var (fromDate, toDate) = GetEffectiveDailySummaryRange();

        LoadErrorMessage = null;
        IsLoading = true;
        IsRebuildingDailySummaries = true;

        try
        {
            var result = await AttendanceDailySummaryService.RebuildAsync(
                new RebuildAttendanceDailySummaryRequest(fromDate, toDate),
                disposalTokenSource.Token);

            ToastService.ShowSuccess(
                $"Đã tổng hợp {result.RebuiltSummaryCount:N0} dòng ngày công từ {result.TotalPunchCount:N0} lượt chấm công.");

            await LoadSummariesAsync(showLoading: false);
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể tổng hợp dữ liệu attendance_daily_summaries.");
        }
        finally
        {
            IsRebuildingDailySummaries = false;
            IsLoading = false;
        }
    }

    private (DateOnly FromDate, DateOnly ToDate) GetEffectiveDailySummaryRange()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var workDate = ToolbarDate.HasValue
            ? DateOnly.FromDateTime(ToolbarDate.Value.Date)
            : today;

        return (workDate, workDate);
    }

    private async Task<IReadOnlyList<AttendanceDeviceRecord>> GetActiveAttendanceDevicesWithSerialAsync()
    {
        try
        {
            var devices = await AttendanceDeviceDataProvider.GetAsync(disposalTokenSource.Token);
            return devices
                .Where(device => device.IsInUse && !string.IsNullOrWhiteSpace(device.SerialNumber))
                .DistinctBy(device => device.SerialNumber)
                .ToList();
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }

            return [];
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể tải danh sách máy chấm công để tạo lệnh.");
            return [];
        }
    }

    private static string BuildAttendanceLogQueryCommandContent(DateTime attDate)
    {
        var startTime = $"{attDate:yyyy-MM-dd} 00:00:00";
        var endTime = $"{attDate:yyyy-MM-dd} 23:59:59";

        return $"DATA QUERY ATTLOG StartTime={startTime}\tEndTime={endTime}";
    }

    private Task OpenDetailPopupAsync(AttendanceDailySummaryRecord summary)
    {
        DetailSummary = summary;
        IsDetailPopupVisible = true;

        return Task.CompletedTask;
    }

    private async Task ResetFiltersAsync()
    {
        ToolbarDate = DateTime.Today;

        await ReloadAsync();
    }

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("attendance-daily-summaries"),
        "Đã bắt đầu xuất Excel cho dữ liệu tổng kết.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "attendance-daily-summaries-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("attendance-daily-summaries"),
        "Đã bắt đầu xuất PDF cho dữ liệu tổng kết.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "attendance-daily-summaries-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if(Grid is null)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu attendance_daily_summaries.");
        }
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        DetailSummary = null;
        IsDetailPopupVisible = false;

        if(Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private List<AttendanceDailySummaryRecord> GetSelectedSummaries()
    {
        var visibleIds = Summaries.Select(summary => summary.Id).ToHashSet();

        return SelectedDataItems
            .OfType<AttendanceDailySummaryRecord>()
            .Where(summary => visibleIds.Contains(summary.Id))
            .DistinctBy(summary => summary.Id)
            .ToList();
    }

    private AttendanceDailySummaryFilter BuildFilter()
    {
        var workDate = ToolbarDate.HasValue
            ? DateOnly.FromDateTime(ToolbarDate.Value.Date)
            : (DateOnly?)null;

        return new AttendanceDailySummaryFilter(
            workDate,
            workDate,
            SearchText,
            SearchResultLimit);
    }

    private string FormatDate(DateOnly value) => value.ToString("dd/MM/yyyy", DisplayCulture);

    private string FormatPunchMoments(AttendanceDailySummaryRecord summary) =>
        summary.PunchMoments.Count == 0
            ? "--"
            : string.Join(" | ", summary.PunchMoments.Select(FormatPunchMoment));

    private static string FormatPunchMoment(string value)
    {
        if(TimeOnly.TryParse(value, out var time))
        {
            return time.ToString("HH:mm");
        }

        return value.Length >= 5 ? value[..5] : value;
    }

    private static AttendanceDailySummaryRecord MapRecord(AttendanceDailySummaryListItemDto row) =>
        new()
        {
            Id = row.Id,
            EmployeeId = row.EmployeeId,
            EmployeeCode = row.EmployeeCode,
            EmployeeName = row.EmployeeName,
            DepartmentName = row.DepartmentName,
            PositionName = row.PositionName,
            WorkDate = row.WorkDate,
            PunchCount = row.PunchCount,
            PunchMomentsText = row.PunchMomentsText,
            FirstPunchTime = row.FirstPunchTime,
            LastPunchTime = row.LastPunchTime,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc
        };

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }
}
