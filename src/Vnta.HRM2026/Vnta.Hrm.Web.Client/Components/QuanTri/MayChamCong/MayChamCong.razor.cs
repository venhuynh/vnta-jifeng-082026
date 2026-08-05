using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.MayChamCong {
    public partial class MayChamCong : IDisposable {
        static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
        readonly CancellationTokenSource disposalTokenSource = new();

        [Inject]
        AttendanceDeviceDataProvider DataProvider { get; set; } = default!;

        [Inject]
        IHrmDialogService DialogService { get; set; } = default!;

        [Inject]
        IHrmToastService ToastService { get; set; } = default!;

        [Inject]
        IAdmsDeviceCommandService DeviceCommandService { get; set; } = default!;

        IReadOnlyList<AttendanceDeviceRecord> Devices { get; set; } = [];
        IReadOnlyList<object> SelectedDataItems { get; set; } = [];
        IReadOnlyList<AttendanceDeviceInfoRow> DetailRows { get; set; } = [];
        IGrid? Grid { get; set; }
        string? SearchText { get; set; }
        string? LoadErrorMessage { get; set; }
        string? EditErrorMessage { get; set; }
        string? DetailErrorMessage { get; set; }
        string DetailEmptyMessage { get; set; } =
            "Hãy dùng menu Tạo lệnh, chọn Truy vấn thông tin và mở lại chi tiết sau khi thiết bị phản hồi.";
        AttendanceDeviceRecord? DetailDevice { get; set; }
        DateTime? DetailResponseTime { get; set; }
        bool IsLoading { get; set; } = true;
        bool IsCreatingNewDevice { get; set; }
        bool IsCommandProcessing { get; set; }
        bool IsDetailPopupVisible { get; set; }
        bool IsDetailLoading { get; set; }
        long DetailLoadVersion { get; set; }

        bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
        bool HasEditError => !string.IsNullOrWhiteSpace(EditErrorMessage);
        bool CanInteract => !IsLoading && !HasLoadError;
        bool CanCreate => CanInteract;
        bool CanEditSelected => CanInteract && GetSelectedDeviceCount() == 1;
        bool CanDeleteSelected => CanInteract && GetSelectedDeviceCount() > 0;
        bool CanCreateCommand =>
            CanInteract
            && !IsCommandProcessing
            && GetSelectedDevicesWithSerial().Count > 0;
        bool CanExport => !IsLoading && Devices.Count > 0;
        bool CanExportSelected => CanExport && GetSelectedDeviceCount() > 0;

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if(firstRender) {
                await ReloadAsync();
                await InvokeAsync(StateHasChanged);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        async Task ReloadAsync() {
            if(disposalTokenSource.IsCancellationRequested)
                return;

            LoadErrorMessage = null;
            EditErrorMessage = null;
            IsLoading = true;

            try {
                await ClearSelectionAsync();
                Devices = await DataProvider.GetAsync(disposalTokenSource.Token);
            } catch(OperationCanceledException) {
                if(!disposalTokenSource.IsCancellationRequested)
                    throw;
            } catch(Exception) {
                Devices = [];
                LoadErrorMessage = "Có lỗi khi tải dữ liệu máy chấm công. Vui lòng thử lại.";
                ToastService.ShowError("Không thể tải danh sách máy chấm công.");
            } finally {
                IsLoading = false;
            }
        }

        Task OnSelectedDataItemsChanged(IReadOnlyList<object> items) {
            SelectedDataItems = items;
            return Task.CompletedTask;
        }

        async Task OnAddDeviceClick() {
            if(!CanCreate || Grid is null)
                return;

            EditErrorMessage = null;
            await Grid.StartEditNewRowAsync();
        }

        async Task OnEditDeviceClick() {
            if(Grid is null)
                return;

            var device = GetSingleSelectedDevice();
            if(device is null) {
                ToastService.ShowWarning("Hãy chọn đúng một máy chấm công để điều chỉnh.");
                return;
            }

            EditErrorMessage = null;
            await Grid.StartEditDataItemAsync(device, nameof(AttendanceDeviceRecord.Code));
        }

        async Task OnDeleteDevicesClick() {
            var selectedDevices = GetSelectedDevices();
            if(selectedDevices.Count == 0) {
                ToastService.ShowWarning("Hãy chọn ít nhất một máy chấm công để xóa.");
                return;
            }

            var confirmed = await DialogService.ConfirmAsync(
                selectedDevices.Count == 1
                    ? $"Bạn có chắc muốn xóa máy `{selectedDevices[0].Code}`?"
                    : $"Bạn có chắc muốn xóa {selectedDevices.Count} máy chấm công đã chọn?",
                title: "Xác nhận xóa",
                okText: "Xóa",
                cancelText: "Hủy",
                renderStyle: MessageBoxRenderStyle.Danger);

            if(!confirmed)
                return;

            try {
                Devices = await DataProvider.DeleteAsync(selectedDevices.Select(device => device.Id), disposalTokenSource.Token);
                await ClearSelectionAsync();
                ToastService.ShowSuccess("Đã xóa máy chấm công đã chọn.");
            } catch(OperationCanceledException) {
                if(!disposalTokenSource.IsCancellationRequested)
                    throw;
            } catch(Exception) {
                ToastService.ShowError("Không thể xóa máy chấm công đã chọn.");
            }
        }

        async Task OnQueryDeviceInfoClick() {
            var selectedDevices = GetSelectedDevicesWithSerial();
            if(selectedDevices.Count == 0) {
                ToastService.ShowWarning("Hãy chọn ít nhất một máy chấm công có số serial.");
                return;
            }

            await CreateDeviceCommandAsync(
                selectedDevices,
                "INFO",
                BuildCommandSuccessMessage(selectedDevices.Count, "truy vấn thông tin"),
                "Không thể tạo lệnh truy vấn thông tin.");
        }

        async Task OnRebootDeviceClick() {
            var selectedDevices = GetSelectedDevicesWithSerial();
            if(selectedDevices.Count == 0) {
                ToastService.ShowWarning("Hãy chọn ít nhất một máy chấm công có số serial.");
                return;
            }

            var confirmationMessage = selectedDevices.Count == 1
                ? $"Bạn có chắc muốn tạo lệnh khởi động lại máy `{GetDeviceDisplayName(selectedDevices[0])}`?"
                : $"Bạn có chắc muốn tạo lệnh khởi động lại {selectedDevices.Count} máy chấm công đã chọn?";
            var confirmed = await DialogService.ConfirmAsync(
                confirmationMessage,
                title: "Xác nhận khởi động lại",
                okText: "Khởi động lại",
                cancelText: "Hủy",
                renderStyle: MessageBoxRenderStyle.Warning);

            if(!confirmed)
                return;

            await CreateDeviceCommandAsync(
                selectedDevices,
                "REBOOT",
                BuildCommandSuccessMessage(selectedDevices.Count, "khởi động lại"),
                "Không thể tạo lệnh khởi động lại.");
        }

        async Task OpenDeviceDetailAsync(AttendanceDeviceRecord device) {
            DetailDevice = device;
            IsDetailPopupVisible = true;
            await LoadDeviceDetailAsync(device);
        }

        Task RetryDeviceDetailAsync() {
            return DetailDevice is null
                ? Task.CompletedTask
                : LoadDeviceDetailAsync(DetailDevice);
        }

        void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

        async Task OnCancelDeviceEditClick() {
            if(Grid is not null)
                await Grid.CancelEditAsync();
        }

        Task ExportAllDataToExcel() => ExportAsync(
            () => Grid!.ExportToXlsxAsync("attendance-devices"),
            "Đã bắt đầu xuất Excel.");

        Task ExportSelectedRowsToExcel() => ExportAsync(
            () => Grid!.ExportToXlsxAsync(
                "attendance-devices-selected",
                new GridXlExportOptions { ExportSelectedRowsOnly = true }),
            "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

        Task ExportAllDataToPdf() => ExportAsync(
            () => Grid!.ExportToPdfAsync("attendance-devices"),
            "Đã bắt đầu xuất PDF.");

        Task ExportSelectedRowsToPdf() => ExportAsync(
            () => Grid!.ExportToPdfAsync(
                "attendance-devices-selected",
                new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
            "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

        async Task ExportAsync(Func<Task> exportAction, string successMessage) {
            if(Grid is null) {
                ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
                return;
            }

            try {
                await exportAction();
                ToastService.ShowInfo(successMessage);
            } catch(Exception) {
                ToastService.ShowError("Không thể xuất dữ liệu máy chấm công.");
            }
        }

        async Task CreateDeviceCommandAsync(
            IReadOnlyList<AttendanceDeviceRecord> devices,
            string content,
            string successMessage,
            string errorMessage) {
            if(IsCommandProcessing || devices.Count == 0)
                return;

            IsCommandProcessing = true;
            try {
                foreach(var device in devices) {
                    var serialNumber = device.SerialNumber;
                    if(string.IsNullOrWhiteSpace(serialNumber))
                        continue;

                    await DeviceCommandService.CreateAsync(
                        new UpsertAdmsDeviceCommandRequest(
                            serialNumber,
                            content,
                            null,
                            content),
                        disposalTokenSource.Token);
                }

                ToastService.ShowSuccess(successMessage);
            } catch(OperationCanceledException) {
                if(!disposalTokenSource.IsCancellationRequested)
                    throw;
            } catch(Exception) {
                ToastService.ShowError(errorMessage);
            } finally {
                IsCommandProcessing = false;
            }
        }

        async Task LoadDeviceDetailAsync(AttendanceDeviceRecord device) {
            var loadVersion = ++DetailLoadVersion;
            DetailRows = [];
            DetailResponseTime = null;
            DetailErrorMessage = null;
            IsDetailLoading = false;
            DetailEmptyMessage =
                "Hãy dùng menu Tạo lệnh, chọn Truy vấn thông tin và mở lại chi tiết sau khi thiết bị phản hồi.";

            var serialNumber = device.SerialNumber;
            if(string.IsNullOrWhiteSpace(serialNumber)) {
                DetailErrorMessage = "Máy chấm công chưa có số serial để truy vấn thông tin.";
                return;
            }

            IsDetailLoading = true;
            try {
                var response = await DeviceCommandService.GetLatestInfoResponseAsync(
                    serialNumber,
                    disposalTokenSource.Token);

                if(loadVersion != DetailLoadVersion)
                    return;

                if(response is null)
                    return;

                DetailResponseTime = response.ResponseTime;
                DetailRows = response.Items
                    .Select(item => new AttendanceDeviceInfoRow(
                        item.NormalizedKey,
                        AttendanceDeviceInfoLabelMapper.GetLabel(item.NormalizedKey, item.Key),
                        item.Value))
                    .ToArray();

                if(DetailRows.Count == 0) {
                    DetailEmptyMessage = "Thiết bị đã phản hồi nhưng chưa có thông tin chi tiết có thể hiển thị.";
                }
            } catch(OperationCanceledException) {
                if(!disposalTokenSource.IsCancellationRequested)
                    throw;
            } catch(Exception) {
                if(loadVersion == DetailLoadVersion) {
                    DetailErrorMessage = "Có lỗi khi đọc phản hồi INFO của máy chấm công. Vui lòng thử lại.";
                    ToastService.ShowError("Không thể tải thông tin chi tiết máy chấm công.");
                }
            } finally {
                if(loadVersion == DetailLoadVersion)
                    IsDetailLoading = false;
            }
        }

        void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e) {
            EditErrorMessage = null;
            IsCreatingNewDevice = e.IsNew;
            var model = (AttendanceDeviceRecord)e.EditModel;

            if(e.IsNew) {
                InitializeNewDeviceDefaults(model);
            }
        }

        async Task OnEditModelSaving(GridEditModelSavingEventArgs e) {
            EditErrorMessage = null;

            try {
                var editModel = (AttendanceDeviceRecord)e.EditModel;
                NormalizeEditModel(editModel);
                EnsureInternalCode(editModel, e.IsNew);

                var activationCodeValidationMessage = ValidateActivationCode(editModel);
                if(!string.IsNullOrWhiteSpace(activationCodeValidationMessage)) {
                    EditErrorMessage = activationCodeValidationMessage;
                    e.Cancel = true;
                    return;
                }

                var now = DateTime.UtcNow;
                if(editModel.CreatedAtUtc == default)
                    editModel.CreatedAtUtc = now;
                editModel.UpdatedAtUtc = now;

                var validationMessage = await DataProvider.ValidateAsync(editModel, disposalTokenSource.Token);
                if(!string.IsNullOrWhiteSpace(validationMessage)) {
                    EditErrorMessage = validationMessage;
                    e.Cancel = true;
                    return;
                }

                Devices = await DataProvider.SaveAsync(editModel, e.IsNew, disposalTokenSource.Token);
                e.Reload = false;
                await ClearSelectionAsync();
                ToastService.ShowSuccess(e.IsNew ? "Đã thêm máy chấm công." : "Đã cập nhật máy chấm công.");
            } catch(OperationCanceledException) {
                if(!disposalTokenSource.IsCancellationRequested)
                    throw;

                e.Cancel = true;
            } catch(InvalidOperationException ex) {
                EditErrorMessage = ex.Message;
                e.Cancel = true;
                ToastService.ShowError("Không thể lưu máy chấm công.");
            } catch(Exception) {
                EditErrorMessage = "Không thể lưu dữ liệu máy chấm công. Vui lòng kiểm tra lại thông tin.";
                e.Cancel = true;
                ToastService.ShowError("Không thể lưu máy chấm công.");
            }
        }

        string GetUsageBadgeCssClass(bool isInUse) => isInUse
            ? "usage-badge usage-badge-active"
            : "usage-badge usage-badge-inactive";

        string GetUsageBadgeText(bool isInUse) => isInUse ? "Đang dùng" : "Ngừng dùng";

        string FormatDateTime(DateTime? value) {
            if(!value.HasValue)
                return "--";

            var normalized = value.Value.Kind == DateTimeKind.Unspecified
                ? value.Value
                : value.Value.ToLocalTime();

            return normalized.ToString("dd/MM/yyyy HH:mm", DisplayCulture);
        }

        async Task ClearSelectionAsync() {
            SelectedDataItems = [];

            if(Grid is null)
                return;

            await Grid.DeselectAllAsync();
            Grid.SetFocusedRowIndex(-1);
        }

        List<AttendanceDeviceRecord> GetSelectedDevices() => SelectedDataItems.OfType<AttendanceDeviceRecord>().ToList();

        List<AttendanceDeviceRecord> GetSelectedDevicesWithSerial() =>
            GetSelectedDevices()
                .Where(device => !string.IsNullOrWhiteSpace(device.SerialNumber))
                .ToList();

        AttendanceDeviceRecord? GetSingleSelectedDevice() {
            var selectedDevices = GetSelectedDevices();
            return selectedDevices.Count == 1 ? selectedDevices[0] : null;
        }

        int GetSelectedDeviceCount() => GetSelectedDevices().Count;

        static string GetDeviceDisplayName(AttendanceDeviceRecord device) =>
            string.IsNullOrWhiteSpace(device.Name)
                ? device.SerialNumber ?? "Máy chấm công"
                : device.Name;

        static string BuildCommandSuccessMessage(int deviceCount, string commandName) =>
            deviceCount == 1
                ? $"Đã tạo lệnh {commandName} cho máy chấm công."
                : $"Đã tạo lệnh {commandName} cho {deviceCount} máy chấm công.";

        static void NormalizeEditModel(AttendanceDeviceRecord model) {
            model.Code = NormalizeNullable(model.Code);
            model.Name = NormalizeNullable(model.Name);
            model.SerialNumber = NormalizeSerial(model.SerialNumber);
            model.Location = NormalizeNullable(model.Location);
            model.IpAddress = NormalizeNullable(model.IpAddress);
            model.MacAddress = NormalizeNullable(model.MacAddress);
            model.ActivationCode = NormalizeNullable(model.ActivationCode);
            model.VendorName = NormalizeNullable(model.VendorName);
            model.DeviceModel = NormalizeNullable(model.DeviceModel);
            model.FirmwareVersion = NormalizeNullable(model.FirmwareVersion);
            model.FingerprintVersion = NormalizeNullable(model.FingerprintVersion);
            model.TimeZone = NormalizeNullable(model.TimeZone);
            model.AttendanceLogStamp = NormalizeNullable(model.AttendanceLogStamp);
            model.AttendancePhotoStamp = NormalizeNullable(model.AttendancePhotoStamp);
            model.OperationLogStamp = NormalizeNullable(model.OperationLogStamp);
            model.ErrorLogStamp = NormalizeNullable(model.ErrorLogStamp);
            model.TransferFlag = NormalizeNullable(model.TransferFlag);
            model.Delay = NormalizeNullable(model.Delay);
            model.Realtime = NormalizeNullable(model.Realtime);
            model.TransInterval = NormalizeNullable(model.TransInterval);
            model.TransTimes = NormalizeNullable(model.TransTimes);
            model.Encrypt = NormalizeNullable(model.Encrypt);
            model.ErrorDelay = NormalizeNullable(model.ErrorDelay);
            model.IrTempDetectionFunOn = NormalizeNullable(model.IrTempDetectionFunOn);
            model.MaskDetectionFunOn = NormalizeNullable(model.MaskDetectionFunOn);
            model.MultiBioDataSupport = NormalizeNullable(model.MultiBioDataSupport);

            if(model.Port <= 0)
                model.Port = null;
        }

        static void InitializeNewDeviceDefaults(AttendanceDeviceRecord model) {
            var utcNow = DateTime.UtcNow;

            model.Id = Guid.NewGuid();
            model.Name = "VNTA-Devices";
            model.IsInUse = true;
            model.Delay = "10";
            model.FirmwareVersion = string.Empty;
            model.IpAddress = "192.168.1.201";
            model.MacAddress = "0C:00:00:00:B1:02";
            model.Encrypt = "0";
            model.Realtime = "1";
            model.SyncTime = 0;
            model.ErrorDelay = "120";
            model.Timeout = 120;
            model.TransInterval = "30";
            model.TransTimes = string.Empty;
            model.VendorName = "ZK";
            model.TransferFlag = "TransData AttLog\tOpLog\tAttPhoto\tEnrollUser\tChgUser\tEnrollFP\tChgFP\tFPImag\tFACE\tUserPic\tWORKCODE\tBioPhoto";
            model.AttendanceLogStamp = "0";
            model.AttendancePhotoStamp = "0";
            model.OperationLogStamp = "0";
            model.TimeZone = "07:00";
            model.LastRequestTime = DateTime.Now;
            model.IrTempDetectionFunOn = "0";
            model.MaskDetectionFunOn = "0";
            model.MultiBioDataSupport = "1:1:1:1:1:1:1:1:1:1";
            model.UserCount = 0;
            model.AttendanceLogCount = 0;
            model.FingerprintCount = 0;
            model.DeviceModel = string.Empty;
            model.Status = 0;
            model.CreatedAtUtc = utcNow;
            model.UpdatedAtUtc = utcNow;
        }

        static void EnsureInternalCode(AttendanceDeviceRecord model, bool isNew) {
            if(!isNew && !string.IsNullOrWhiteSpace(model.Code))
                return;

            model.Code = BuildInternalCode(model);
        }

        static string? NormalizeNullable(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        static string? NormalizeSerial(string? value) {
            if(string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = AttendanceGatewayActivationCode.NormalizeSerial(value);
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        static string BuildInternalCode(AttendanceDeviceRecord model) {
            var source = NormalizeNullable(model.SerialNumber)
                ?? NormalizeNullable(model.Name)
                ?? "VNTA-Devices";

            var normalized = new string(source
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());

            if(normalized.Length == 0)
                normalized = "VNTADEVICE";

            if(normalized.Length > 40)
                normalized = normalized[..40];

            var suffix = model.Id == Guid.Empty
                ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()
                : model.Id.ToString("N")[..8].ToUpperInvariant();

            return $"DEV-{normalized}-{suffix}";
        }

        static string? ValidateActivationCode(AttendanceDeviceRecord model) {
            if(string.IsNullOrWhiteSpace(model.SerialNumber))
                return null;

            if(string.IsNullOrWhiteSpace(model.ActivationCode)) {
                return "Mã kích hoạt không được để trống.";
            }

            if(!AttendanceGatewayActivationCode.HasExpectedShape(model.ActivationCode))
                return "Mã kích hoạt phải đúng dạng VN1-XXXX-XXXX-XXXX-XXXX.";

            if(!AttendanceGatewayActivationCode.Validate(model.SerialNumber, model.ActivationCode))
                return "Mã kích hoạt không đúng với số serial này nên chưa thể lưu.";
            return null;
        }

        public void Dispose() {
            disposalTokenSource.Cancel();
            disposalTokenSource.Dispose();
        }
    }
}
