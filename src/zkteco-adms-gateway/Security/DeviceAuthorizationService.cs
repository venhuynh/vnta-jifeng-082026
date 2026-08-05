using System.Text;
using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Hubs;
using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;
using Vnta.AttendanceGateway.Protocol.Parsers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Vnta.AttendanceGateway.Security;

public class DeviceAuthorizationService
{
    private const string AuthorizationDeviceStoreUnavailableEventType = "authorization-device-store-unavailable";
    private const string UnnamedAttendanceDeviceName = "Máy chấm công chưa đặt tên";

    private readonly ILogger<DeviceAuthorizationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly RealtimeGatewayLogPublisher _realtimeAdmsLogPublisher;
    private readonly AdmsActivityPublisher _admsActivityPublisher;

    public DeviceAuthorizationService(
        ILogger<DeviceAuthorizationService> logger,
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        IHubContext<DeviceHub> hubContext,
        RealtimeGatewayLogPublisher realtimeAdmsLogPublisher,
        AdmsActivityPublisher admsActivityPublisher)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _cache = cache;
        _hubContext = hubContext;
        _realtimeAdmsLogPublisher = realtimeAdmsLogPublisher;
        _admsActivityPublisher = admsActivityPublisher;
    }

    public async Task<DeviceAuthorizationResult> AuthorizeAsync(string method, string url, string connectionId, string flowId, CancellationToken cancellationToken = default)
    {
        var sn = HeaderParser.ExtractQueryParam(url, "SN");
        var receivedAtUtc = DateTimeOffset.UtcNow;
        var receivedAtVietnam = VietnamTime.Now.DateTime;
        if (string.IsNullOrWhiteSpace(sn))
        {
            _logger.LogWarning("VNTA Attendance Gateway FLOW AUTHORIZE [{FlowId}] Reject because SN is missing. Method={Method}, Url={Url}", flowId, method, url);
            await _realtimeAdmsLogPublisher.PublishAsync(
                null,
                null,
                method,
                url,
                null,
                "du_lieu_la",
                "Serial Number is missing",
                cancellationToken,
                direction: "event",
                eventType: "authorization-missing-serial",
                flowId: flowId,
                connectionId: connectionId);
            return new DeviceAuthorizationResult
            {
                IsAuthorized = false,
                FailureResponse = Encoding.ASCII.GetBytes("HTTP/1.1 401 Unauthorized\r\n\r\n")
            };
        }

        var normalizedSerial = VntaCrypto.NormalizeSerial(sn);
        var cacheKey = $"DeviceActivation_{normalizedSerial}";
        var deviceExists = false;
        var isActivated = false;
        string? deviceName = null;

        if (_cache.TryGetValue(cacheKey, out bool cachedActivationStatus))
        {
            deviceExists = true;
            isActivated = cachedActivationStatus;
            _logger.LogInformation("VNTA Attendance Gateway FLOW AUTHORIZE [{FlowId}] Loaded activation from cache. SN={SN}, IsActivated={Status}", flowId, normalizedSerial, isActivated);
        }
        else
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
            if (!await ZktecoSchemaGuard.TableExistsAsync(dbContext, "devices", cancellationToken))
            {
                _logger.LogError("VNTA Attendance Gateway FLOW AUTHORIZE [{FlowId}] Cannot authorize because table 'devices' is missing. SN={SN}", flowId, normalizedSerial);
                return await RejectAuthorizationAsync(
                    normalizedSerial,
                    deviceName,
                    method,
                    url,
                    connectionId,
                    flowId,
                    receivedAtUtc,
                    "authorization-device-store-unavailable",
                    "Kho dữ liệu thiết bị chưa sẵn sàng vì thiếu bảng devices.",
                    "503 Service Unavailable",
                    cancellationToken);
            }

            try
            {
                var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.SerialNumber == normalizedSerial, cancellationToken);
                if (device is not null)
                {
                    deviceExists = true;
                    isActivated = VntaCrypto.ValidateActivationCode(normalizedSerial, device.ActivationCode ?? string.Empty);
                    deviceName = string.IsNullOrWhiteSpace(device.Name) ? null : device.Name;

                    _cache.Set(cacheKey, isActivated, TimeSpan.FromMinutes(5));
                    _logger.LogInformation("VNTA Attendance Gateway FLOW AUTHORIZE [{FlowId}] Resolved device from database. SN={SN}, IsActivated={Status}", flowId, normalizedSerial, isActivated);
                }
                else
                {
                    _logger.LogWarning("VNTA Attendance Gateway FLOW AUTHORIZE [{FlowId}] Device not found in database. SN={SN}", flowId, normalizedSerial);
                }
            }
            catch (PostgresException ex) when (IsMissingRelation(ex, "devices"))
            {
                _logger.LogError(ex, "VNTA Attendance Gateway FLOW AUTHORIZE [{FlowId}] Cannot authorize because table 'devices' is missing. SN={SN}", flowId, normalizedSerial);
                return await RejectAuthorizationAsync(
                    normalizedSerial,
                    deviceName,
                    method,
                    url,
                    connectionId,
                    flowId,
                    receivedAtUtc,
                    "authorization-device-store-unavailable",
                    "Kho dữ liệu thiết bị chưa sẵn sàng vì thiếu bảng devices.",
                    "503 Service Unavailable",
                    cancellationToken);
            }
        }

        if (!deviceExists)
        {
            deviceName ??= UnnamedAttendanceDeviceName;

            await _realtimeAdmsLogPublisher.PublishAsync(
                normalizedSerial,
                deviceName,
                method,
                url,
                null,
                "thiet_bi_chua_dang_ky",
                "Device was not found in the database",
                cancellationToken,
                direction: "event",
                eventType: "authorization-device-not-found",
                flowId: flowId,
                connectionId: connectionId);

            await _admsActivityPublisher.PublishAsync(
                normalizedSerial,
                deviceName,
                method,
                url,
                "authorization-device-not-found",
                "rejected",
                "Thiết bị chưa được đăng ký trong ADMS gateway nên yêu cầu bị từ chối.",
                null,
                flowId,
                connectionId,
                cancellationToken,
                rejectionReason: "Device was not found in the database",
                persistAsSystemLog: false);

            await _hubContext.Clients.All.SendAsync(
                "DeviceConnectionEvent",
                new
                {
                    sn = normalizedSerial,
                    deviceName,
                    status = "rejected",
                    phase = "authorization",
                    requestMethod = method,
                    requestUrl = url,
                    receivedAtUtc,
                    reason = "Device was not found in the database"
                },
                cancellationToken);

            return new DeviceAuthorizationResult
            {
                IsAuthorized = false,
                FailureResponse = Encoding.ASCII.GetBytes("HTTP/1.1 401 Unauthorized\r\n\r\n")
            };
        }

        if (!isActivated)
        {
            await _realtimeAdmsLogPublisher.PublishAsync(
                normalizedSerial,
                deviceName,
                method,
                url,
                null,
                "thiet_bi_chua_kich_hoat",
                "Activation code is invalid for this device serial",
                cancellationToken,
                direction: "event",
                eventType: "authorization-device-not-activated",
                flowId: flowId,
                connectionId: connectionId);

            await _hubContext.Clients.All.SendAsync(
                "DeviceConnectionEvent",
                new
                {
                    sn = normalizedSerial,
                    deviceName,
                    status = "rejected",
                    phase = "authorization",
                    requestMethod = method,
                    requestUrl = url,
                    receivedAtUtc,
                    reason = "Activation code is invalid for this device serial"
                },
                cancellationToken);

            return new DeviceAuthorizationResult
            {
                IsAuthorized = false,
                FailureResponse = Encoding.ASCII.GetBytes("HTTP/1.1 403 Forbidden\r\n\r\n")
            };
        }

        deviceName = await TouchAuthorizedDeviceAsync(normalizedSerial, receivedAtVietnam, deviceName, flowId, cancellationToken);

        _logger.LogInformation("VNTA Attendance Gateway FLOW AUTHORIZE [{FlowId}] Authorized. SN={SN}, DeviceName={DeviceName}", flowId, normalizedSerial, deviceName ?? "<empty>");

        await _hubContext.Clients.All.SendAsync(
            "DeviceConnectionEvent",
            new
            {
                sn = normalizedSerial,
                deviceName,
                status = "authorized",
                phase = "authorization",
                requestMethod = method,
                requestUrl = url,
                receivedAtUtc,
                reason = "Authorized device request received by VNTA Attendance Gateway"
            },
            cancellationToken);

        await _admsActivityPublisher.PublishAsync(
            normalizedSerial,
            deviceName,
            method,
            url,
            "authorization-authorized",
            "authorized",
            "Thiết bị đã vượt qua kiểm tra ủy quyền và được phép tiếp tục giao tiếp.",
            null,
            flowId,
            connectionId,
            cancellationToken,
            persistAsSystemLog: false);

        return new DeviceAuthorizationResult
        {
            IsAuthorized = true,
            Device = new DeviceAuthorizationContext(normalizedSerial)
        };
    }
    private async Task<string?> TouchAuthorizedDeviceAsync(
        string normalizedSerial,
        DateTime receivedAtVietnam,
        string? currentDeviceName,
        string flowId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        if (!await ZktecoSchemaGuard.TableExistsAsync(dbContext, "devices", cancellationToken))
        {
            _logger.LogWarning("VNTA Attendance Gateway FLOW DB [{FlowId}] Skipped device heartbeat update because table 'devices' is missing. SN={SN}", flowId, normalizedSerial);
            return currentDeviceName;
        }

        try
        {
            var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.SerialNumber == normalizedSerial, cancellationToken);
            if (device is null)
            {
                return currentDeviceName;
            }

            device.LastRequestTime = receivedAtVietnam;
            device.UpdatedAtUtc = VietnamTime.Now.DateTime;

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("VNTA Attendance Gateway FLOW DB [{FlowId}] Updated device heartbeat. SN={SN}, LastRequestTime={LastRequestTime}", flowId, normalizedSerial, receivedAtVietnam);

            if (!string.IsNullOrWhiteSpace(device.Name))
            {
                return device.Name;
            }
        }
        catch (PostgresException ex) when (IsMissingRelation(ex, "devices"))
        {
            _logger.LogWarning(ex, "VNTA Attendance Gateway FLOW DB [{FlowId}] Skipped device heartbeat update because table 'devices' is missing. SN={SN}", flowId, normalizedSerial);
            return currentDeviceName;
        }

        return currentDeviceName;
    }

    private async Task<DeviceAuthorizationResult> RejectAuthorizationAsync(
        string? normalizedSerial,
        string? deviceName,
        string method,
        string url,
        string connectionId,
        string flowId,
        DateTimeOffset receivedAtUtc,
        string eventType,
        string reason,
        string httpStatusLine,
        CancellationToken cancellationToken)
    {
        await _realtimeAdmsLogPublisher.PublishAsync(
            normalizedSerial,
            deviceName,
            method,
            url,
            null,
            "he_thong_chua_san_sang",
            reason,
            cancellationToken,
            direction: "event",
            eventType: eventType,
            flowId: flowId,
            connectionId: connectionId);

        // Luồng "Luồng hoạt động" của HRM đang đọc semantic stream riêng.
        // Với nhánh store chưa sẵn sàng, team vẫn muốn người vận hành thấy ngay
        // một dòng dễ hiểu thay vì phải tự suy luận từ raw event kỹ thuật.
        if (string.Equals(eventType, AuthorizationDeviceStoreUnavailableEventType, StringComparison.OrdinalIgnoreCase))
        {
            await _admsActivityPublisher.PublishAsync(
                normalizedSerial,
                deviceName,
                method,
                url,
                eventType,
                "rejected",
                "Thiết bị chưa được kích hoạt",
                reason,
                flowId,
                connectionId,
                cancellationToken,
                rejectionReason: reason,
                persistAsSystemLog: false);
        }

        await _hubContext.Clients.All.SendAsync(
            "DeviceConnectionEvent",
            new
            {
                sn = normalizedSerial,
                deviceName,
                status = "rejected",
                phase = "authorization",
                requestMethod = method,
                requestUrl = url,
                receivedAtUtc,
                reason
            },
            cancellationToken);

        return new DeviceAuthorizationResult
        {
            IsAuthorized = false,
            FailureResponse = Encoding.ASCII.GetBytes($"HTTP/1.1 {httpStatusLine}\r\n\r\n")
        };
    }

    private static bool IsMissingRelation(PostgresException ex, string relationName) =>
        ex.SqlState == PostgresErrorCodes.UndefinedTable
        && ex.MessageText.Contains(relationName, StringComparison.OrdinalIgnoreCase);
}

