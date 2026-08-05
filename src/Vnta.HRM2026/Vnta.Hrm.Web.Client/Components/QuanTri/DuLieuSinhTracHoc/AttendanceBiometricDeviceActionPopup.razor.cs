using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Attendance;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.DuLieuSinhTracHoc;

public partial class AttendanceBiometricDeviceActionPopup : IDisposable
{
    internal static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    private readonly CancellationTokenSource disposalTokenSource = new();
    private bool wasVisible;

    [Inject]
    private AttendanceDeviceDataProvider DeviceDataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    private IGrid? DeviceGrid { get; set; }
    private IReadOnlyList<AttendanceDeviceRecord> Devices { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public IReadOnlyList<AttendanceBiometricDataRecord> SelectedEmployees { get; set; } = [];

    [Parameter]
    public AttendanceBiometricDeviceActionType ActionType { get; set; }

    [Parameter]
    public bool IsSubmitting { get; set; }

    [Parameter]
    public EventCallback<AttendanceBiometricDeviceActionSubmitRequest> SubmitRequested { get; set; }

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

    private bool CanSubmit =>
        Visible
        && !IsLoading
        && !IsSubmitting
        && !HasLoadError
        && SelectedEmployeeCount > 0
        && GetSelectedDevicesWithSerial().Count > 0;

    private string PopupSubtitle => ActionType switch
    {
        AttendanceBiometricDeviceActionType.Pull =>
            $"Loại lệnh: Đồng bộ từ máy chấm công cho {SelectedEmployeeCount:N0} nhân viên.",
        AttendanceBiometricDeviceActionType.Push =>
            $"Loại lệnh: Cập nhật lên máy chấm công cho {SelectedEmployeeCount:N0} nhân viên.",
        AttendanceBiometricDeviceActionType.DeleteOnDevice =>
            $"Loại lệnh: Xóa dữ liệu trên máy chấm công cho {SelectedEmployeeCount:N0} nhân viên.",
        _ =>
            $"Loại lệnh: Thao tác máy cho {SelectedEmployeeCount:N0} nhân viên."
    };

    private string HeaderText => ActionType switch
    {
        AttendanceBiometricDeviceActionType.Pull => "Tạo lệnh đồng bộ từ máy chấm công",
        AttendanceBiometricDeviceActionType.Push => "Tạo lệnh cập nhật lên máy chấm công",
        AttendanceBiometricDeviceActionType.DeleteOnDevice => "Tạo lệnh xóa dữ liệu trên máy chấm công",
        _ => "Tạo lệnh máy chấm công"
    };

    private int SelectedEmployeeCount =>
        SelectedEmployees
            .Where(static employee => employee.EmployeeId != Guid.Empty)
            .GroupBy(static employee => employee.EmployeeId)
            .Count();

    private string SubmitButtonText => ActionType switch
    {
        AttendanceBiometricDeviceActionType.Pull => "Đồng bộ từ máy",
        AttendanceBiometricDeviceActionType.Push => "Cập nhật lên máy",
        AttendanceBiometricDeviceActionType.DeleteOnDevice => "Xóa trên máy",
        _ => "Tạo lệnh"
    };

    private string SubmitButtonDisplayText => IsSubmitting
        ? "Đang tạo lệnh..."
        : SubmitButtonText;

    private string SubmitButtonIconUrl => ActionType switch
    {
        AttendanceBiometricDeviceActionType.Pull => VntaDevExpressIcons.Download,
        AttendanceBiometricDeviceActionType.Push => VntaDevExpressIcons.Upload,
        AttendanceBiometricDeviceActionType.DeleteOnDevice => VntaDevExpressIcons.Delete,
        _ => VntaDevExpressIcons.Command
    };

    private ButtonRenderStyle SubmitButtonRenderStyle => ActionType switch
    {
        AttendanceBiometricDeviceActionType.DeleteOnDevice => ButtonRenderStyle.Danger,
        _ => ButtonRenderStyle.Primary
    };

    protected override async Task OnParametersSetAsync()
    {
        if (Visible && !wasVisible)
        {
            wasVisible = true;
            await ResetAndLoadAsync();
            return;
        }

        if (!Visible && wasVisible)
        {
            wasVisible = false;
            ResetPopupState();
        }
    }

    private async Task ResetAndLoadAsync()
    {
        ResetPopupState();
        await LoadDevicesAsync();
    }

    private void ResetPopupState()
    {
        SearchText = null;
        LoadErrorMessage = null;
        Devices = [];
        SelectedDataItems = [];
        IsLoading = false;
    }

    private async Task LoadDevicesAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        LoadErrorMessage = null;
        IsLoading = true;

        try
        {
            var devices = await DeviceDataProvider.GetAsync(disposalTokenSource.Token);
            Devices = devices
                .Where(static device => device.IsInUse)
                .OrderBy(static device => device.SerialNumber, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static device => device.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
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
            Devices = [];
            LoadErrorMessage = "Không tải được danh sách máy chấm công đang sử dụng. Vui lòng thử lại.";
            ToastService.ShowError("Không tải được danh sách máy chấm công đang sử dụng.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task OnVisibleChanged(bool visible)
    {
        return VisibleChanged.InvokeAsync(visible);
    }

    private Task CloseAsync()
    {
        return VisibleChanged.InvokeAsync(false);
    }

    private Task RetryAsync()
    {
        return LoadDevicesAsync();
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private async Task SubmitAsync()
    {
        if (SelectedEmployeeCount == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một nhân viên để tạo lệnh.");
            return;
        }

        var selectedDevices = GetSelectedDevicesWithSerial();
        if (selectedDevices.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một máy chấm công để tạo lệnh.");
            return;
        }

        await SubmitRequested.InvokeAsync(
            new AttendanceBiometricDeviceActionSubmitRequest(
                ActionType,
                SelectedEmployees.ToArray(),
                selectedDevices));
    }

    private List<AttendanceDeviceRecord> GetSelectedDevicesWithSerial() =>
        SelectedDataItems
            .OfType<AttendanceDeviceRecord>()
            .Where(static device => !string.IsNullOrWhiteSpace(device.SerialNumber))
            .DistinctBy(static device => device.Id)
            .ToList();

    private static string GetUsageBadgeCssClass(bool isInUse) => isInUse
        ? "usage-badge usage-badge-active"
        : "usage-badge usage-badge-inactive";

    private static string GetUsageBadgeText(bool isInUse) => isInUse ? "Đang dùng" : "Ngừng dùng";

    private static string FormatDateTime(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        var normalized = value.Value.Kind == DateTimeKind.Unspecified
            ? value.Value
            : value.Value.ToLocalTime();

        return normalized.ToString("dd/MM/yyyy HH:mm", DisplayCulture);
    }

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }
}
