using System.Text;
using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Hubs;
using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public class HandshakeHandler : IRequestHandler
{
    private const string ServerVersion = "2.2.14";
    private const string PushProtocolVersion = "2.4.1";
    private const string PushOptionsFlag = "1";

    private readonly ILogger<HandshakeHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly AdmsActivityPublisher _admsActivityPublisher;

    public bool RequiresDeviceAuthorization => true;

    public HandshakeHandler(
        ILogger<HandshakeHandler> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<DeviceHub> hubContext,
        AdmsActivityPublisher admsActivityPublisher)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _admsActivityPublisher = admsActivityPublisher;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "GET" && url.Contains("/iclock/cdata") && url.Contains("options=all");
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var normalizedSerial = requestContext.Device?.SerialNumber ?? string.Empty;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.SerialNumber == normalizedSerial, cancellationToken);
        if (device is null)
        {
            _logger.LogWarning("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Handshake failed because device not found after authorization. SN={SN}", requestContext.FlowId, normalizedSerial);
            return Encoding.ASCII.GetBytes("HTTP/1.1 401 Unauthorized\r\n\r\n");
        }

        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Handshake accepted. SN={SN}", requestContext.FlowId, normalizedSerial);

        await _hubContext.Clients.All.SendAsync(
            "DeviceConnectionEvent",
            new
            {
                sn = normalizedSerial,
                deviceName = string.IsNullOrWhiteSpace(device.Name) ? null : device.Name,
                status = "accepted",
                phase = "handshake",
                requestMethod = requestContext.Method,
                requestUrl = requestContext.Url,
                receivedAtUtc = DateTimeOffset.UtcNow,
                reason = "Device started configuration sync"
            },
            cancellationToken);

        await _admsActivityPublisher.PublishAsync(
            normalizedSerial,
            string.IsNullOrWhiteSpace(device.Name) ? null : device.Name,
            requestContext.Method,
            requestContext.Url,
            "handshake-accepted",
            "accepted",
            "Thiết bị đã bắt tay thành công và bắt đầu đồng bộ cấu hình.",
            requestContext.BodyRawText,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken,
            persistAsSystemLog: false);

        var replyBuilder = new StringBuilder();
        replyBuilder.AppendLine($"GET OPTION FROM:{device.SerialNumber}");
        replyBuilder.AppendLine($"Stamp={device.AttendanceLogStamp ?? "0"}");
        replyBuilder.AppendLine($"OpStamp={device.OperationLogStamp ?? "0"}");
        replyBuilder.AppendLine($"PhotoStamp={device.AttendancePhotoStamp ?? "0"}");
        replyBuilder.AppendLine($"TransFlag={device.TransferFlag ?? "1111000000"}");
        replyBuilder.AppendLine($"ErrorDelay={device.ErrorDelay ?? "60"}");
        replyBuilder.AppendLine($"Delay={device.Delay ?? "10"}");
        replyBuilder.AppendLine($"TimeZone={device.TimeZone ?? "07:00"}");
        replyBuilder.AppendLine($"TransTimes={device.TransTimes ?? string.Empty}");
        replyBuilder.AppendLine($"TransInterval={device.TransInterval ?? "1"}");
        replyBuilder.AppendLine($"SyncTime={device.SyncTime}");
        replyBuilder.AppendLine($"Realtime={device.Realtime ?? "1"}");
        replyBuilder.AppendLine($"Encrypt={device.Encrypt ?? "0"}");
        replyBuilder.AppendLine($"ServerVer={ServerVersion} {DateTime.Now:MM/dd/yyyy}");
        replyBuilder.AppendLine($"PushProtVer={PushProtocolVersion}");
        replyBuilder.AppendLine($"PushOptionsFlag={PushOptionsFlag}");
        replyBuilder.AppendLine($"ATTLOGStamp={device.AttendanceLogStamp ?? "0"}");
        replyBuilder.AppendLine($"OPERLOGStamp={device.OperationLogStamp ?? "0"}");
        replyBuilder.AppendLine($"ATTPHOTOStamp={device.AttendancePhotoStamp ?? "0"}");
        replyBuilder.AppendLine("ServerName=Logtime Server");
        replyBuilder.AppendLine($"MultiBioDataSupport={device.MultiBioDataSupport ?? "1:1:1:1:1:1:1:1:1:1"}");

        return ZktecoResponseBuilder.BuildHttpResponse(replyBuilder.ToString());
    }
}
