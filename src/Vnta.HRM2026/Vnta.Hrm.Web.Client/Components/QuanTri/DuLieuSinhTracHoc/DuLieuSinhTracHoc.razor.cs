using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Models.Attendance;
using Vnta.Hrm.Web.Client.Services.Adms;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.DuLieuSinhTracHoc;

public partial class DuLieuSinhTracHoc : IDisposable
{
    private const int SearchResultLimit = 5000;
    private const string SummaryAllKey = "all";
    private const string SummaryFingerprintKey = "fingerprint";
    private const string SummaryFaceKey = "face";
    private const string SummaryCardKey = "card";
    private const string SummaryPasswordKey = "password";
    private const string SummaryAdminKey = "admin";

    private readonly CancellationTokenSource disposalTokenSource = new();

    [Inject]
    private IAttendanceBiometricDataReadService AttendanceBiometricDataReadService { get; set; } = default!;

    [Inject]
    private IAttendanceBiometricDataRefreshService AttendanceBiometricDataRefreshService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private IHrmDialogService DialogService { get; set; } = default!;

    [Inject]
    private IAttendanceBiometricDeviceCommandService AttendanceBiometricDeviceCommandService { get; set; } = default!;

    private IReadOnlyList<AttendanceBiometricDataRecord> Rows { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IReadOnlyList<BiometricSummaryBadge> SummaryBadges { get; set; } = BuildSummaryBadges([]);
    private IReadOnlyList<AttendanceBiometricDataRecord> DeviceActionEmployees { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsRefreshing { get; set; }
    private bool IsDeviceActionPopupVisible { get; set; }
    private bool IsDeviceActionSubmitting { get; set; }
    private AttendanceBiometricDeviceActionType CurrentDeviceActionType { get; set; } = AttendanceBiometricDeviceActionType.Pull;

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanInteract => !IsLoading && !IsRefreshing && !HasLoadError;
    private bool CanRefresh => !IsLoading && !IsRefreshing;
    private bool CanExport => !IsLoading && !IsRefreshing && VisibleRows.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedRows().Count > 0;
    private bool ShowGridLoadingPanel => IsLoading || IsRefreshing;
    private IReadOnlyList<AttendanceBiometricDataRecord> VisibleRows => ApplySummaryFilter(Rows, ActiveSummaryBadgeKey);
    private string LoadingText => IsRefreshing
        ? "Đang tổng hợp dữ liệu sinh trắc học..."
        : HrmUiDefaults.LoadingText;
    private string EmptyStateTitle => string.IsNullOrWhiteSpace(SearchText)
        ? "Chưa có dữ liệu sinh trắc học"
        : "Không tìm thấy dữ liệu sinh trắc học phù hợp";
    private string EmptyStateMessage => string.IsNullOrWhiteSpace(SearchText)
        ? "Danh sách dữ liệu sinh trắc học sẽ hiển thị tại đây khi dữ liệu đã được đồng bộ vào hệ thống."
        : "Hãy thử từ khóa khác hoặc xóa bộ lọc tìm kiếm để xem thêm dữ liệu.";
    private string EmptyStateActionText => string.IsNullOrWhiteSpace(SearchText)
        ? "Tải lại"
        : "Xóa tìm kiếm";

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
        await base.OnInitializedAsync();
    }

    private async Task ReloadAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        LoadErrorMessage = null;
        IsLoading = true;

        try
        {
            await ClearSelectionAsync();
            var result = await AttendanceBiometricDataReadService.SearchAsync(
                new AttendanceBiometricDataFilter(null, null, null, SearchResultLimit),
                disposalTokenSource.Token);

            Rows = result.Select(MapRecord).ToList();
            SummaryBadges = BuildSummaryBadges(Rows);
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            Rows = [];
            SummaryBadges = BuildSummaryBadges([]);
            LoadErrorMessage = "Có lỗi khi tải dữ liệu sinh trắc học. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách dữ liệu sinh trắc học.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshBiometricDataAsync()
    {
        if (disposalTokenSource.IsCancellationRequested || !CanRefresh)
        {
            return;
        }

        LoadErrorMessage = null;
        IsRefreshing = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await AttendanceBiometricDataRefreshService.RefreshAsync(disposalTokenSource.Token);
            ToastService.ShowSuccess(BuildRefreshSummaryMessage(result));
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể tổng hợp lại dữ liệu sinh trắc học.");
        }
        finally
        {
            IsRefreshing = false;
            await ReloadAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OnSummaryBadgeClick(string badgeKey)
    {
        ActiveSummaryBadgeKey = badgeKey;
        await ClearSelectionAsync();
    }

    private async Task OpenDeviceActionPopupAsync(AttendanceBiometricDeviceActionType actionType)
    {
        var selectedRows = GetSelectedRows();
        if (selectedRows.Count == 0)
        {
            ToastService.ShowWarning(GetSelectEmployeeWarningMessage(actionType));
            return;
        }

        CurrentDeviceActionType = actionType;
        DeviceActionEmployees = selectedRows;
        IsDeviceActionPopupVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    private Task OnDeviceActionPopupVisibleChanged(bool visible)
    {
        IsDeviceActionPopupVisible = visible;
        if (!visible)
        {
            IsDeviceActionSubmitting = false;
            DeviceActionEmployees = [];
        }

        return Task.CompletedTask;
    }

    private async Task OnDeviceActionSubmitAsync(AttendanceBiometricDeviceActionSubmitRequest request)
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsDeviceActionSubmitting = true;

        try
        {
            switch (request.ActionType)
            {
                case AttendanceBiometricDeviceActionType.Pull:
                {
                    var result = await AttendanceBiometricDeviceCommandService.CreatePullCommandsAsync(
                        request.Employees,
                        request.Devices,
                        disposalTokenSource.Token);

                    IsDeviceActionPopupVisible = false;
                    DeviceActionEmployees = [];

                    ToastService.ShowSuccess(
                        BuildPullCommandSuccessMessage(
                            result.CommandsCreated,
                            result.MatchedEmployees,
                            result.DeviceCount));
                    break;
                }
                case AttendanceBiometricDeviceActionType.Push:
                {
                    var result = await AttendanceBiometricDeviceCommandService.CreatePushCommandsAsync(
                        request.Employees,
                        request.Devices,
                        disposalTokenSource.Token);

                    IsDeviceActionPopupVisible = false;
                    DeviceActionEmployees = [];

                    ToastService.ShowSuccess(
                        BuildPushCommandSuccessMessage(
                            result.CommandsCreated,
                            result.MatchedEmployees,
                            result.DeviceCount));
                    break;
                }
                case AttendanceBiometricDeviceActionType.DeleteOnDevice:
                {
                    var confirmed = await ConfirmDeleteDeviceActionAsync(request);
                    if (!confirmed)
                    {
                        return;
                    }

                    var result = await AttendanceBiometricDeviceCommandService.CreateDeleteCommandsAsync(
                        request.Employees,
                        request.Devices,
                        disposalTokenSource.Token);

                    IsDeviceActionPopupVisible = false;
                    DeviceActionEmployees = [];

                    ToastService.ShowSuccess(
                        BuildDeleteCommandSuccessMessage(
                            result.CommandsCreated,
                            result.MatchedEmployees,
                            result.DeviceCount));
                    break;
                }
                default:
                    ToastService.ShowWarning("Loại thao tác máy hiện chưa được hỗ trợ.");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError(GetDeviceActionErrorMessage(request.ActionType));
        }
        finally
        {
            IsDeviceActionSubmitting = false;
        }
    }

    private void OpenEmployeeDetail(AttendanceBiometricDataRecord _)
    {
        ToastService.ShowWarning("Chức năng xem chi tiết sinh trắc học hiện chưa sẵn sàng.");
    }

    private Task DeleteRowAsync(AttendanceBiometricDataRecord _)
    {
        ToastService.ShowWarning("Chức năng xóa dữ liệu sinh trắc học hiện chưa sẵn sàng.");
        return Task.CompletedTask;
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private async Task OnEmptyStateActionClick()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await ReloadAsync();
            return;
        }

        SearchText = null;
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("attendance-biometric-data"),
        "Đã bắt đầu xuất Excel danh sách dữ liệu sinh trắc học.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "attendance-biometric-data-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đang chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("attendance-biometric-data"),
        "Đã bắt đầu xuất PDF danh sách dữ liệu sinh trắc học.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "attendance-biometric-data-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất PDF cho các dòng đang chọn.");

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if (Grid is null)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu sinh trắc học.");
        }
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];

        if (Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private List<AttendanceBiometricDataRecord> GetSelectedRows() =>
        SelectedDataItems
            .OfType<AttendanceBiometricDataRecord>()
            .Where(IsVisibleRow)
            .DistinctBy(row => row.Id)
            .ToList();

    private bool IsVisibleRow(AttendanceBiometricDataRecord row) =>
        VisibleRows.Any(current => current.Id == row.Id);

    private static AttendanceBiometricDataRecord MapRecord(AttendanceBiometricDataListItemDto row) =>
        new()
        {
            Id = row.Id,
            EmployeeId = row.EmployeeId,
            EmployeeCode = row.EmployeeCode,
            EmployeeName = row.EmployeeName,
            Avatar = row.AvatarDataUrl,
            DepartmentName = row.DepartmentName,
            PositionName = row.PositionName,
            FpQty = row.FpQty,
            HasFaceData = row.HasFaceData,
            LastUpdated = row.LastUpdated,
            CardNumber = row.CardNumber,
            IsAdmin = row.IsAdmin,
            HasPassword = row.HasPassword
        };

    private static IReadOnlyList<AttendanceBiometricDataRecord> ApplySummaryFilter(
        IReadOnlyList<AttendanceBiometricDataRecord> rows,
        string badgeKey)
    {
        return badgeKey switch
        {
            SummaryFingerprintKey => rows.Where(row => row.FpQty > 0).ToList(),
            SummaryFaceKey => rows.Where(row => row.HasFaceData).ToList(),
            SummaryCardKey => rows.Where(row => !string.IsNullOrWhiteSpace(row.CardNumber)).ToList(),
            SummaryPasswordKey => rows.Where(row => row.HasPassword).ToList(),
            SummaryAdminKey => rows.Where(row => row.IsAdmin).ToList(),
            _ => rows
        };
    }

    private static IReadOnlyList<BiometricSummaryBadge> BuildSummaryBadges(
        IReadOnlyList<AttendanceBiometricDataRecord> rows)
    {
        return
        [
            new(SummaryAllKey, "Tổng số nhân viên", rows.Count),
            new(SummaryFingerprintKey, "Có vân tay", rows.Count(row => row.FpQty > 0)),
            new(SummaryFaceKey, "Có gương mặt", rows.Count(row => row.HasFaceData)),
            new(SummaryCardKey, "Có thẻ", rows.Count(row => !string.IsNullOrWhiteSpace(row.CardNumber))),
            new(SummaryPasswordKey, "Có mật khẩu", rows.Count(row => row.HasPassword)),
            new(SummaryAdminKey, "Quản trị", rows.Count(row => row.IsAdmin))
        ];
    }

    private static string BuildRefreshSummaryMessage(AttendanceBiometricDataRefreshResult result)
    {
        return
            $"Đã đồng bộ dữ liệu sinh trắc học cho {result.TotalEmployees:N0} nhân sự. " +
            $"Mới: {result.ProfilesInserted:N0}, cập nhật: {result.ProfilesUpdated:N0}, xóa: {result.ProfilesDeleted:N0}.";
    }

    private static string GetSelectEmployeeWarningMessage(AttendanceBiometricDeviceActionType actionType)
    {
        return actionType switch
        {
            AttendanceBiometricDeviceActionType.Pull =>
                "Hãy chọn ít nhất một nhân viên để tạo lệnh đồng bộ từ máy chấm công.",
            AttendanceBiometricDeviceActionType.Push =>
                "Hãy chọn ít nhất một nhân viên để tạo lệnh cập nhật lên máy chấm công.",
            AttendanceBiometricDeviceActionType.DeleteOnDevice =>
                "Hãy chọn ít nhất một nhân viên để tạo lệnh xóa dữ liệu trên máy chấm công.",
            _ =>
                "Hãy chọn ít nhất một nhân viên trước khi thao tác với máy chấm công."
        };
    }

    private static string BuildPullCommandSuccessMessage(int commandsCreated, int matchedEmployees, int deviceCount)
    {
        return
            $"Đã tạo {commandsCreated:N0} lệnh đồng bộ dữ liệu sinh trắc học " +
            $"từ {deviceCount:N0} máy chấm công cho {matchedEmployees:N0} nhân viên.";
    }


    private static string BuildPushCommandSuccessMessage(int commandsCreated, int matchedEmployees, int deviceCount)
    {
        return
            $"Đã tạo {commandsCreated:N0} lệnh cập nhật dữ liệu sinh trắc học " +
            $"lên {deviceCount:N0} máy chấm công cho {matchedEmployees:N0} nhân viên.";
    }

    private static string BuildDeleteCommandSuccessMessage(int commandsCreated, int matchedEmployees, int deviceCount)
    {
        return
            $"Đã tạo {commandsCreated:N0} lệnh xóa dữ liệu sinh trắc học " +
            $"trên {deviceCount:N0} máy chấm công cho {matchedEmployees:N0} nhân viên.";
    }

    private static string GetDeviceActionErrorMessage(AttendanceBiometricDeviceActionType actionType)
    {
        return actionType switch
        {
            AttendanceBiometricDeviceActionType.Pull =>
                "Không thể tạo lệnh đồng bộ dữ liệu sinh trắc học từ máy chấm công.",
            AttendanceBiometricDeviceActionType.Push =>
                "Không thể tạo lệnh cập nhật dữ liệu sinh trắc học lên máy chấm công.",
            AttendanceBiometricDeviceActionType.DeleteOnDevice =>
                "Không thể tạo lệnh xóa dữ liệu sinh trắc học trên máy chấm công.",
            _ =>
                "Không thể tạo lệnh thao tác máy cho dữ liệu sinh trắc học."
        };
    }

    private async Task<bool> ConfirmDeleteDeviceActionAsync(AttendanceBiometricDeviceActionSubmitRequest request)
    {
        var employeeCount = request.Employees
            .Select(static employee => employee.EmployeeId)
            .Where(static employeeId => employeeId != Guid.Empty)
            .Distinct()
            .Count();
        var deviceCount = request.Devices
            .Select(static device => device.SerialNumber)
            .Where(static serialNumber => !string.IsNullOrWhiteSpace(serialNumber))
            .Select(static serialNumber => serialNumber!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var message =
            "Bạn sắp tạo lệnh xóa dữ liệu sinh trắc học trên thiết bị.\n\n" +
            $"Nhân viên đã chọn: {employeeCount:N0}\n" +
            $"Máy chấm công: {deviceCount:N0}\n\n" +
            "Dữ liệu sinh trắc học lưu trong hệ thống không bị xóa. " +
            "Các bản ghi trên máy sẽ bị xóa khi thiết bị nhận và xử lý lệnh.\n\n" +
            "Bạn có muốn tiếp tục không?";

        return await DialogService.ConfirmAsync(
            message,
            "Xác nhận tạo lệnh xóa trên máy",
            "Tiếp tục tạo lệnh",
            "Hủy",
            MessageBoxRenderStyle.Danger);
    }

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }

    private sealed record BiometricSummaryBadge(string Key, string Label, int Count);
}
