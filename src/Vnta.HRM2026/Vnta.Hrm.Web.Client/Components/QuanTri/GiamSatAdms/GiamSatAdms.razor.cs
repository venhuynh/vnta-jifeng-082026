using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Services.Adms;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.GiamSatAdms;

public partial class GiamSatAdms : ComponentBase, IAsyncDisposable {
    // Màn hình này gom ba góc nhìn khác nhau của cùng một dòng dữ liệu realtime:
    // 1. Dock trái: trạng thái thiết bị.
    // 2. Grid giữa: hoạt động semantic đã đọc được ngữ nghĩa.
    // 3. Panel dưới: payload raw để đối soát khi cần soi sâu.
    // Mỗi vùng của màn hình này là một góc nhìn độc lập:
    // 1. Khối trái nhận luồng trạng thái thiết bị.
    // 2. Khối giữa nhận luồng semantic activity.
    // 3. Khối dưới nhận luồng raw exchange.
    // Màn này chỉ là view quan sát, không tạo liên kết chọn-dòng giữa các khối.

    #region Hằng số kỹ thuật

    // Gateway có thể phát các event vòng đời theo tên cố định.
    // Giữ thành hằng số để tránh rải string literal khắp file và giúp việc so sánh nhất quán.
    private const string ConnectionOpenedEventType = "connection-opened";
    private const string ConnectionClosedEventType = "connection-closed";

    // Dock trái không được dùng ConnectionId làm định danh hiển thị.
    // Khi chưa đủ thông tin nhận diện, toàn UI sẽ fallback về cùng một nhãn này.
    private const string UnknownDeviceName = "Thiết bị chưa xác định";

    // Quy ước vận hành hiện tại: nếu thiết bị im lặng quá 3 phút thì xem là offline.
    private const int DefaultDeviceOfflineTimeoutSeconds = 180;

    // Grid "Sự kiện tức thời" chỉ nên giữ một cửa sổ đủ nhỏ để người vận hành
    // theo dõi realtime mà không làm client phình bộ đệm quá mức.
    private const int MaxVisibleActivityRows = 50;
    private const int MaxVisibleRawEvents = 50;

    // Chu kỳ kiểm tra không cần quá dày vì trạng thái thiết bị không phải số liệu mili giây.
    private static readonly TimeSpan DeviceStatusRefreshInterval = TimeSpan.FromSeconds(5);

    #endregion

    #region Trường nội bộ

    // Đây là các bộ đệm trạng thái của client.
    // Chúng đồng thời là nguồn bind trực tiếp cho UI, nên mọi cập nhật phải giữ được tính nhất quán.
    private List<AdmsDeviceGridRow> deviceRows = [];
    private List<AdmsActivityGridRow> activityRows = [];
    private List<AttendanceGatewayRealtimeEventDto> rawEvents = [];

    // Token này dùng chung cho toàn bộ vòng đời component để dừng hub và vòng timer sạch sẽ khi dispose.
    private readonly CancellationTokenSource disposalTokenSource = new();

    private HubConnection? hubConnection;
    private Task? deviceStatusLoopTask;
    private AdmsGatewayMonitorOptions monitorOptions = new();
    private bool IsDisposed { get; set; }

    #endregion

    #region Dependency inject

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IOptions<AdmsGatewayMonitorOptions> MonitorOptionsAccessor { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    #endregion

    #region Trạng thái bind cho UI

    protected IReadOnlyList<AdmsDeviceGridRow> DeviceRows => deviceRows;
    protected IReadOnlyList<AdmsActivityGridRow> ActivityRows => activityRows;

    protected bool IsCompactScreen { get; set; }
    protected bool IsInitialLoading { get; set; } = true;
    protected bool IsReconnectInProgress { get; set; }
    protected string? ConnectionErrorMessage { get; set; }
    protected string RawPanelText { get; set; } = string.Empty;
    protected AdmsGatewayStatus GatewayStatus { get; set; } = AdmsGatewayStatus.Unknown;
    protected DateTimeOffset? LastGatewayMessageAtUtc { get; set; }

    protected bool IsBusy => IsInitialLoading || IsReconnectInProgress;
    private bool CanMutateUi => !IsDisposed && !disposalTokenSource.IsCancellationRequested;

    protected string GatewayStatusText => GatewayStatus switch {
        AdmsGatewayStatus.Running => "Đang chạy",
        AdmsGatewayStatus.Reconnecting => "Đang kết nối lại",
        AdmsGatewayStatus.Disconnected => "Mất kết nối",
        _ => "Chưa xác định"
    };

    protected string GatewayStatusCssClass =>
        $"adms-status-chip adms-status-chip--{GatewayStatus.ToString().ToLowerInvariant()}";

    protected string LastGatewaySignalText => LastGatewayMessageAtUtc is null
        ? "Chưa nhận tín hiệu từ gateway."
        : $"Nhận gần nhất: {LastGatewayMessageAtUtc.Value.LocalDateTime:dd/MM/yyyy HH:mm:ss}";

    protected string DeviceSummaryText => deviceRows.Count == 0
        ? "Chưa có máy chấm công nào xuất hiện."
        : $"{deviceRows.Count(row => row.IsOnline)} trực tuyến / {deviceRows.Count} thiết bị";

    protected string ActivitySummaryText => activityRows.Count == 0
        ? "Chưa có sự kiện semantic từ gateway."
        : $"{activityRows.Count} sự kiện semantic đang hiển thị.";

    protected string RawPanelEmptyText => "Chưa có raw log để hiển thị.";

    #endregion

    #region Vòng đời component và sự kiện UI

    protected override async Task OnInitializedAsync() {
        monitorOptions = MonitorOptionsAccessor.Value ?? new AdmsGatewayMonitorOptions();

        // Khởi động vòng suy luận online/offline trước khi mở hub
        // để ngay cả các event đầu tiên cũng đi vào cùng một pipeline trạng thái.
        deviceStatusLoopTask = RunDeviceStatusLoopAsync(disposalTokenSource.Token);

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if(firstRender) {
            await ConnectToGatewayAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected void OnCompactScreenChanged(bool isCompactScreen) => IsCompactScreen = isCompactScreen;

    protected void OnClearClick() {
        // Chỉ xóa bộ đệm của client để giảm tải UI.
        // Không đụng tới gateway và cũng không xóa log phía server.
        activityRows = [];
        rawEvents = [];
        RawPanelText = string.Empty;

        ToastService.ShowInfo("Đã xóa các log đang hiển thị trên màn ADMS.");
    }

    protected async Task OnReconnectClick() {
        await ConnectToGatewayAsync(forceReconnect: true);
    }

    #endregion

    #region Kết nối thời gian thực với hub nội bộ của HRM

    private async Task ConnectToGatewayAsync(bool forceReconnect = false) {
        // Nếu hub đang trong pha Connecting/Reconnecting mà ta tiếp tục mở thêm vòng connect mới,
        // state của UI rất dễ bị giật giữa nhiều phiên kết nối chồng lên nhau.
        if(hubConnection?.State is HubConnectionState.Connecting or HubConnectionState.Reconnecting) {
            return;
        }

        IsReconnectInProgress = true;
        ConnectionErrorMessage = null;
        await InvokeAsync(RequestRender);

        try {
            if(forceReconnect && hubConnection is not null) {
                // Reconnect chủ động phải bỏ hẳn kết nối cũ để toàn bộ callback được đăng ký lại từ đầu
                // và tránh giữ lại state ngầm của một phiên hub cũ.
                await hubConnection.DisposeAsync();
                hubConnection = null;
            }

            hubConnection ??= CreateHubConnection();

            if(hubConnection.State == HubConnectionState.Connected) {
                await hubConnection.StopAsync(disposalTokenSource.Token);
            }

            await hubConnection.StartAsync(disposalTokenSource.Token);
            await LoadMonitorSnapshotAsync();

            GatewayStatus = AdmsGatewayStatus.Running;
            ConnectionErrorMessage = null;

            if(forceReconnect) {
                ToastService.ShowSuccess("Đã kết nối lại gateway ADMS.");
            }
        }
        catch(Exception ex) {
            GatewayStatus = AdmsGatewayStatus.Disconnected;
            ConnectionErrorMessage = $"Không thể kết nối gateway ADMS: {ex.Message}";

            if(forceReconnect) {
                ToastService.ShowError(ConnectionErrorMessage);
            }
        }
        finally {
            IsInitialLoading = false;
            IsReconnectInProgress = false;
            await InvokeAsync(RequestRender);
        }
    }

    private HubConnection CreateHubConnection() {
        var connection = new HubConnectionBuilder()
            .WithUrl(BuildHubUrl())
            .WithAutomaticReconnect()
            .Build();

        // HRM rebroadcast lại đúng hai event name cũ để UI không phải học thêm contract mới.
        connection.On<AdmsMonitorDeviceStateDto>(
            AdmsMonitorSignalREvents.DeviceConnectionStateEvent,
            HandleDeviceStateReceivedAsync);
        connection.On<AttendanceGatewayRealtimeEventDto>(
            AdmsMonitorSignalREvents.GatewayActivityEvent,
            HandleActivityEventReceivedAsync);
        connection.On<AttendanceGatewayRealtimeEventDto>(
            AdmsMonitorSignalREvents.GatewayRawLogEvent,
            HandleRawEventReceivedAsync);

        connection.Reconnecting += error => InvokeAsync(() => {
            if(!CanMutateUi) {
                return;
            }

            GatewayStatus = AdmsGatewayStatus.Reconnecting;
            IsReconnectInProgress = true;
            ConnectionErrorMessage = error is null
                ? "Kênh realtime ADMS nội bộ đang kết nối lại."
                : $"Kênh realtime ADMS nội bộ đang kết nối lại: {error.Message}";
            RequestRender();
        });

        connection.Reconnected += async _ => {
            await LoadMonitorSnapshotAsync();
            await InvokeAsync(() => {
            if(!CanMutateUi) {
                return;
            }

            GatewayStatus = AdmsGatewayStatus.Running;
            IsReconnectInProgress = false;
            ConnectionErrorMessage = null;
            LastGatewayMessageAtUtc = DateTimeOffset.UtcNow;
            RequestRender();
            });
        };

        connection.Closed += error => InvokeAsync(() => {
            if(!CanMutateUi) {
                return;
            }

            GatewayStatus = AdmsGatewayStatus.Disconnected;
            IsReconnectInProgress = false;
            ConnectionErrorMessage = error is null
                ? "Kênh realtime ADMS nội bộ đã đóng."
                : $"Kênh realtime ADMS nội bộ đã đóng: {error.Message}";
            RequestRender();
        });

        return connection;
    }

    private string BuildHubUrl() {
        // Kiến trúc đích là browser chỉ nối vào host HRM.
        // BaseUrl chỉ còn là option dự phòng nếu sau này hub nội bộ được đặt sau một origin khác.
        if(string.IsNullOrWhiteSpace(monitorOptions.BaseUrl)) {
            return NavigationManager.ToAbsoluteUri(monitorOptions.HubPath).ToString();
        }

        var baseUri = monitorOptions.BaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? monitorOptions.BaseUrl
            : monitorOptions.BaseUrl + "/";
        var hubPath = monitorOptions.HubPath.TrimStart('/');

        return new Uri(new Uri(baseUri, UriKind.Absolute), hubPath).ToString();
    }

    #endregion

    #region Nhận và phân phối dữ liệu realtime

    private async Task LoadMonitorSnapshotAsync() {
        if(hubConnection?.State != HubConnectionState.Connected || !CanMutateUi) {
            return;
        }

        var snapshot = await hubConnection.InvokeAsync<AdmsMonitorSnapshotDto>(
            "GetMonitorSnapshotAsync",
            ResolveActivityBufferLimit(),
            ResolveRawBufferLimit(),
            disposalTokenSource.Token);

        if(!CanMutateUi) {
            return;
        }

        ApplySnapshot(snapshot);
        await InvokeAsync(RequestRender);
    }

    private void ApplySnapshot(AdmsMonitorSnapshotDto snapshot) {
        deviceRows = BuildDeviceRows(snapshot.Devices);
        activityRows = BuildActivityRows(snapshot.ActivityEvents);
        rawEvents = BuildRawEvents(snapshot.RawEvents);
        LastGatewayMessageAtUtc = snapshot.LastReceivedAtUtc;
        RawPanelText = BuildRawPanelText(rawEvents);
    }

    private Task HandleDeviceStateReceivedAsync(AdmsMonitorDeviceStateDto deviceState) =>
        InvokeAsync(() => {
            if(!CanMutateUi) {
                return;
            }

            RegisterGatewaySignal(deviceState.LastSeenAtUtc);
            deviceRows = UpsertDeviceStateRow(deviceRows, deviceState);
            RequestRender();
        });

    private Task HandleActivityEventReceivedAsync(AttendanceGatewayRealtimeEventDto eventDto) =>
        InvokeAsync(() => {
            if(!CanMutateUi) {
                return;
            }

            RegisterGatewaySignal(eventDto.ReceivedAtUtc);

            if(!ShouldDisplayInActivityGrid(eventDto)) {
                RequestRender();
                return;
            }

            activityRows = AppendActivityRow(activityRows, MapActivityRow(eventDto));
            RequestRender();
        });

    private Task HandleRawEventReceivedAsync(AttendanceGatewayRealtimeEventDto eventDto) =>
        InvokeAsync(() => {
            if(!CanMutateUi) {
                return;
            }

            RegisterGatewaySignal(eventDto.ReceivedAtUtc);

            rawEvents = AppendRawEvent(rawEvents, eventDto);
            RawPanelText = BuildRawPanelText(rawEvents);
            RequestRender();
        });

    private void RegisterGatewaySignal(DateTimeOffset receivedAtUtc) {
        LastGatewayMessageAtUtc = receivedAtUtc;
        GatewayStatus = AdmsGatewayStatus.Running;
        ConnectionErrorMessage = null;
    }

    #endregion

    #region Dựng và duy trì trạng thái thiết bị

    private static List<AdmsDeviceGridRow> UpsertDeviceStateRow(
        IReadOnlyList<AdmsDeviceGridRow> currentRows,
        AdmsMonitorDeviceStateDto deviceState) {
        var nextRows = currentRows.ToList();
        var rowIndex = FindDeviceRowIndex(
            nextRows,
            deviceState.DeviceSn,
            ResolveGridDeviceName(deviceState.DeviceName, deviceState.DeviceSn));
        var existingRow = rowIndex >= 0 ? nextRows[rowIndex] : null;
        var updatedRow = CreateDeviceGridRow(deviceState, existingRow);

        if(rowIndex >= 0) {
            nextRows[rowIndex] = updatedRow;
        } else {
            nextRows.Add(updatedRow);
        }

        SortDeviceRows(nextRows);
        return nextRows;
    }

    private static List<AdmsDeviceGridRow> BuildDeviceRows(IEnumerable<AdmsMonitorDeviceStateDto>? devices) {
        var rows = new List<AdmsDeviceGridRow>();
        foreach(var device in devices ?? []) {
            rows = UpsertDeviceStateRow(rows, device);
        }

        return rows;
    }

    private static void SortDeviceRows(List<AdmsDeviceGridRow> rows) {
        // Dock "Thiết bị" phải bám đúng cột "Kết nối gần nhất":
        // thiết bị có tín hiệu mới hơn được đưa lên trước, giảm dần theo thời gian.
        rows.Sort((left, right) => {
            var lastSeenOrder = Nullable.Compare(right.LastSeenAtUtc, left.LastSeenAtUtc);
            if(lastSeenOrder != 0) {
                return lastSeenOrder;
            }

            var statusOrder = right.IsOnline.CompareTo(left.IsOnline);
            if(statusOrder != 0) {
                return statusOrder;
            }

            return string.Compare(left.DeviceDisplayName, right.DeviceDisplayName, StringComparison.OrdinalIgnoreCase);
        });
    }

    #endregion

    #region Dựng và hợp nhất luồng activity semantic

    private static AdmsActivityGridRow MapActivityRow(AttendanceGatewayRealtimeEventDto eventDto) => new() {
        Id = Normalize(eventDto.Id) ?? Guid.NewGuid().ToString("N"),
        FlowId = ResolveFlowId(eventDto),
        ConnectionId = Normalize(eventDto.ConnectionId),
        DeviceSn = Normalize(eventDto.Sn),
        DeviceDisplayName = ResolveActivityDeviceName(eventDto.DeviceName, eventDto.Sn, eventDto.ConnectionId),
        EventType = Normalize(eventDto.EventType) ?? "unknown",
        SummaryText = BuildSummaryText(eventDto),
        RawBody = Normalize(eventDto.RawBody),
        ReceivedAtUtc = eventDto.ReceivedAtUtc
    };

    private static bool ShouldDisplayInActivityGrid(AttendanceGatewayRealtimeEventDto eventDto) =>
        !IsDeviceConnectionLifecycleEvent(eventDto.EventType);

    private List<AdmsActivityGridRow> AppendActivityRow(
        IReadOnlyList<AdmsActivityGridRow> currentRows,
        AdmsActivityGridRow activityRow) {
        if(currentRows.Any(row => string.Equals(row.Id, activityRow.Id, StringComparison.OrdinalIgnoreCase))) {
            return currentRows.ToList();
        }

        var nextRows = currentRows.ToList();
        nextRows.Insert(0, activityRow);
        SortActivityRows(nextRows);
        TrimBuffer(nextRows, ResolveActivityBufferLimit());
        return nextRows;
    }

    private static List<AdmsActivityGridRow> BuildActivityRows(IEnumerable<AttendanceGatewayRealtimeEventDto>? events) {
        var rows = new List<AdmsActivityGridRow>();
        foreach(var activityEvent in (events ?? []).Reverse()) {
            if(!ShouldDisplayInActivityGrid(activityEvent)) {
                continue;
            }

            rows.Insert(0, MapActivityRow(activityEvent));
        }

        SortActivityRows(rows);
        return rows;
    }

    private static void SortActivityRows(List<AdmsActivityGridRow> rows) {
        rows.Sort((left, right) => {
            var receivedOrder = right.ReceivedAtUtc.CompareTo(left.ReceivedAtUtc);
            if(receivedOrder != 0) {
                return receivedOrder;
            }

            return string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string BuildSummaryText(AttendanceGatewayRealtimeEventDto eventDto) {
        var summary = Normalize(eventDto.SummaryText);
        if(!string.IsNullOrWhiteSpace(summary)) {
            return summary;
        }

        // Nếu gateway chưa gửi semantic summary riêng, lấy dòng đầu của raw làm preview.
        // Mục tiêu là để grid giữa vẫn có mô tả ngắn gọn mà không cần mở panel raw.
        var rawPreview = Normalize(eventDto.RawBody);
        if(string.IsNullOrWhiteSpace(rawPreview)) {
            return "Không có mô tả chi tiết.";
        }

        var firstLine = rawPreview
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if(string.IsNullOrWhiteSpace(firstLine)) {
            return "Không có mô tả chi tiết.";
        }

        return firstLine.Length <= 160 ? firstLine : firstLine[..160] + "...";
    }

    #endregion

    #region Dựng và lọc panel raw

    private List<AttendanceGatewayRealtimeEventDto> AppendRawEvent(
        IReadOnlyList<AttendanceGatewayRealtimeEventDto> currentRows,
        AttendanceGatewayRealtimeEventDto eventDto) {
        var eventId = Normalize(eventDto.Id);
        if(!string.IsNullOrWhiteSpace(eventId) &&
           currentRows.Any(row => string.Equals(row.Id, eventId, StringComparison.OrdinalIgnoreCase))) {
            return currentRows.ToList();
        }

        var nextRows = currentRows.ToList();
        nextRows.Insert(0, eventDto);
        TrimBuffer(nextRows, ResolveRawBufferLimit());
        return nextRows;
    }

    private List<AttendanceGatewayRealtimeEventDto> BuildRawEvents(IEnumerable<AttendanceGatewayRealtimeEventDto>? events) {
        var rows = new List<AttendanceGatewayRealtimeEventDto>();
        foreach(var rawEvent in (events ?? []).Reverse()) {
            rows = AppendRawEvent(rows, rawEvent);
        }

        return rows;
    }

    private string BuildRawPanelText(IReadOnlyList<AttendanceGatewayRealtimeEventDto> events) {
        var rawBlocks = events
            .OrderByDescending(raw => raw.ReceivedAtUtc)
            .Take(ResolveRawPanelEventLimit())
            .Select(FormatRawBlock)
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToList();

        return string.Join(Environment.NewLine + Environment.NewLine, rawBlocks);
    }

    private string FormatRawBlock(AttendanceGatewayRealtimeEventDto rawEvent) {
        var header =
            $"{rawEvent.ReceivedAtUtc.LocalDateTime:dd/MM/yyyy HH:mm:ss} | {ResolveActivityDeviceName(rawEvent.DeviceName, rawEvent.Sn, rawEvent.ConnectionId)} | {rawEvent.EventType}";
        var body = Normalize(rawEvent.RawBody);

        return string.IsNullOrWhiteSpace(body) ? header : $"{header}{Environment.NewLine}{body}";
    }

    #endregion

    #region Theo dõi trạng thái online/offline

    private async Task RunDeviceStatusLoopAsync(CancellationToken cancellationToken) {
        using var timer = new PeriodicTimer(DeviceStatusRefreshInterval);

        try {
            while(await timer.WaitForNextTickAsync(cancellationToken)) {
                var timeout = TimeSpan.FromSeconds(monitorOptions.DeviceOfflineTimeoutSeconds > 0
                    ? monitorOptions.DeviceOfflineTimeoutSeconds
                    : DefaultDeviceOfflineTimeoutSeconds);
                var now = DateTimeOffset.UtcNow;
                var nextRows = RefreshDeviceOnlineState(deviceRows, now, timeout);

                if(nextRows is not null) {
                    await InvokeAsync(() => {
                        if(!CanMutateUi) {
                            return;
                        }

                        deviceRows = nextRows;
                        RequestRender();
                    });
                }
            }
        }
        catch(OperationCanceledException) {
        }
    }

    #endregion

    #region Helper nhận diện và chuẩn hóa dữ liệu

    private static List<AdmsDeviceGridRow>? RefreshDeviceOnlineState(
        IReadOnlyList<AdmsDeviceGridRow> currentRows,
        DateTimeOffset now,
        TimeSpan timeout) {
        List<AdmsDeviceGridRow>? nextRows = null;

        for(var index = 0; index < currentRows.Count; index++) {
            var device = currentRows[index];
            var shouldBeOnline = device.LastSeenAtUtc is not null &&
                                 now - device.LastSeenAtUtc.Value < timeout;

            if(device.IsOnline == shouldBeOnline) {
                continue;
            }

            nextRows ??= currentRows.ToList();
            nextRows[index] = CloneDeviceGridRow(device, shouldBeOnline);
        }

        if(nextRows is null) {
            return null;
        }

        SortDeviceRows(nextRows);
        return nextRows;
    }

    private static int FindDeviceRowIndex(IReadOnlyList<AdmsDeviceGridRow> rows, string? deviceSn, string? deviceName) {
        var normalizedDeviceSn = Normalize(deviceSn);
        var normalizedDeviceName = Normalize(deviceName);

        if(!string.IsNullOrWhiteSpace(normalizedDeviceSn)) {
            for(var index = 0; index < rows.Count; index++) {
                if(string.Equals(rows[index].DeviceSn, normalizedDeviceSn, StringComparison.OrdinalIgnoreCase)) {
                    return index;
                }
            }
        }

        if(IsUnresolvedDeviceDisplayName(normalizedDeviceName)) {
            return -1;
        }

        for(var index = 0; index < rows.Count; index++) {
            var row = rows[index];
            if(string.Equals(row.DeviceDisplayName, normalizedDeviceName, StringComparison.OrdinalIgnoreCase) &&
               (string.IsNullOrWhiteSpace(normalizedDeviceSn) ||
                string.IsNullOrWhiteSpace(row.DeviceSn) ||
                string.Equals(row.DeviceSn, normalizedDeviceSn, StringComparison.OrdinalIgnoreCase))) {
                return index;
            }
        }

        return -1;
    }

    private static AdmsDeviceGridRow CreateDeviceGridRow(
        AdmsMonitorDeviceStateDto deviceState,
        AdmsDeviceGridRow? existingRow) => new() {
            RowKey = existingRow?.RowKey ?? Guid.NewGuid().ToString("N"),
            DeviceSn = Normalize(deviceState.DeviceSn),
            DeviceName = ResolvePreferredDeviceName(
                existingRow?.DeviceName,
                deviceState.DeviceName,
                deviceState.DeviceSn),
            LastSeenAtUtc = deviceState.LastSeenAtUtc,
            IsOnline = deviceState.IsOnline
        };

    private static AdmsDeviceGridRow CloneDeviceGridRow(AdmsDeviceGridRow source, bool isOnline) => new() {
        RowKey = source.RowKey,
        DeviceSn = source.DeviceSn,
        DeviceName = source.DeviceName,
        LastSeenAtUtc = source.LastSeenAtUtc,
        IsOnline = isOnline
    };

    private static void TrimBuffer<T>(List<T> items, int maxCount) {
        if(items.Count <= maxCount) {
            return;
        }

        // Dữ liệu mới luôn được chèn lên đầu danh sách.
        // Vì vậy phần bị cắt ở cuối chính là dữ liệu cũ nhất.
        items.RemoveRange(maxCount, items.Count - maxCount);
    }

    private int ResolveActivityBufferLimit() {
        var configuredLimit = monitorOptions.ActivityBufferLimit > 0
            ? monitorOptions.ActivityBufferLimit
            : MaxVisibleActivityRows;

        // Có thể cho cấu hình nhỏ hơn 50 nếu cần tinh gọn hơn,
        // nhưng tuyệt đối không để grid giữ quá 50 dòng theo yêu cầu màn hình hiện tại.
        return Math.Clamp(configuredLimit, 1, MaxVisibleActivityRows);
    }

    private int ResolveRawBufferLimit() {
        var configuredLimit = monitorOptions.RawBufferLimit > 0
            ? monitorOptions.RawBufferLimit
            : MaxVisibleRawEvents;

        return Math.Clamp(configuredLimit, 1, MaxVisibleRawEvents);
    }

    private int ResolveRawPanelEventLimit() {
        var configuredLimit = monitorOptions.RawPanelEventLimit > 0
            ? monitorOptions.RawPanelEventLimit
            : MaxVisibleRawEvents;

        return Math.Clamp(configuredLimit, 1, MaxVisibleRawEvents);
    }

    private static string ResolveActivityDeviceName(string? deviceName, string? deviceSn, string? connectionId) {
        if(!string.IsNullOrWhiteSpace(deviceName)) {
            return deviceName.Trim();
        }

        if(!string.IsNullOrWhiteSpace(deviceSn)) {
            return $"Máy {deviceSn.Trim()}";
        }

        // Grid hoạt động vẫn cho phép hiển thị theo ConnectionId
        // để người vận hành không mất bối cảnh của một phiên kết nối chưa nhận diện xong thiết bị.
        if(!string.IsNullOrWhiteSpace(connectionId)) {
            return $"Kết nối {connectionId.Trim()}";
        }

        return UnknownDeviceName;
    }

    private static string ResolveGridDeviceName(string? deviceName, string? deviceSn) {
        if(!string.IsNullOrWhiteSpace(deviceName)) {
            return deviceName.Trim();
        }

        if(!string.IsNullOrWhiteSpace(deviceSn)) {
            return $"Máy {deviceSn.Trim()}";
        }

        return UnknownDeviceName;
    }

    private static string ResolvePreferredDeviceName(string? currentDeviceName, string? incomingDeviceName, string? deviceSn) {
        if(!string.IsNullOrWhiteSpace(incomingDeviceName)) {
            return incomingDeviceName.Trim();
        }

        if(IsUnresolvedDeviceDisplayName(currentDeviceName)) {
            return ResolveGridDeviceName(incomingDeviceName, deviceSn);
        }

        return currentDeviceName ?? ResolveGridDeviceName(incomingDeviceName, deviceSn);
    }

    private static bool IsUnresolvedDeviceDisplayName(string? deviceName) =>
        string.IsNullOrWhiteSpace(deviceName) ||
        string.Equals(deviceName.Trim(), UnknownDeviceName, StringComparison.OrdinalIgnoreCase);

    private static string? ResolveFlowId(AttendanceGatewayRealtimeEventDto eventDto) =>
        Normalize(eventDto.FlowId) ?? Normalize(eventDto.ConnectionId) ?? Normalize(eventDto.Id);

    private static bool IsDeviceConnectionLifecycleEvent(string? eventType) {
        var normalizedEventType = Normalize(eventType);
        return string.Equals(normalizedEventType, ConnectionOpenedEventType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedEventType, ConnectionClosedEventType, StringComparison.OrdinalIgnoreCase);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void RequestRender() {
        if(CanMutateUi) {
            StateHasChanged();
        }
    }

    #endregion

    #region Giải phóng tài nguyên

    public async ValueTask DisposeAsync() {
        IsDisposed = true;
        disposalTokenSource.Cancel();

        deviceRows = [];
        activityRows = [];
        rawEvents = [];
        RawPanelText = string.Empty;
        ConnectionErrorMessage = null;
        LastGatewayMessageAtUtc = null;
        GatewayStatus = AdmsGatewayStatus.Unknown;

        if(deviceStatusLoopTask is not null) {
            try {
                await deviceStatusLoopTask;
            }
            catch(OperationCanceledException) {
            }
        }

        if(hubConnection is not null) {
            try {
                await hubConnection.StopAsync(CancellationToken.None);
            }
            catch {
            }

            await hubConnection.DisposeAsync();
            hubConnection = null;
        }

        disposalTokenSource.Dispose();
    }

    #endregion

    #region Mô hình hiển thị nội bộ

    protected sealed class AdmsDeviceGridRow {
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
        public string? DeviceSn { get; set; }
        public string DeviceName { get; set; } = UnknownDeviceName;
        public DateTimeOffset? LastSeenAtUtc { get; set; }
        public bool IsOnline { get; set; }

        public string DeviceDisplayName => DeviceName;
        public string DeviceSnLabel => string.IsNullOrWhiteSpace(DeviceSn) ? "Chưa có serial" : $"SN: {DeviceSn}";
        public string StatusText => IsOnline ? "Online" : "Offline";
        public string StatusCssClass => $"adms-status-badge adms-status-badge--{(IsOnline ? "online" : "offline")}";
        public DateTimeOffset? LastSeenAtLocal => LastSeenAtUtc?.ToLocalTime();
    }

    protected sealed class AdmsActivityGridRow {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string? FlowId { get; set; }
        public string? ConnectionId { get; set; }
        public string? DeviceSn { get; set; }
        public string DeviceDisplayName { get; set; } = UnknownDeviceName;
        public string EventType { get; set; } = string.Empty;
        public string SummaryText { get; set; } = string.Empty;
        public string? RawBody { get; set; }
        public DateTimeOffset ReceivedAtUtc { get; set; }

        public DateTimeOffset ReceivedAtLocal => ReceivedAtUtc.ToLocalTime();
        public string DeviceSerialDisplay => string.IsNullOrWhiteSpace(DeviceSn) ? "Chưa có serial" : DeviceSn;
    }

    protected enum AdmsGatewayStatus {
        Unknown,
        Running,
        Reconnecting,
        Disconnected
    }

    #endregion
}
