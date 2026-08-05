using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Protocol.Parsers;
using Microsoft.EntityFrameworkCore;

namespace Vnta.AttendanceGateway.Integration;

public sealed class AttendancePhotoStampSyncService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendancePhotoStampSyncService> _logger;

    public AttendancePhotoStampSyncService(IServiceScopeFactory scopeFactory, ILogger<AttendancePhotoStampSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<AttendancePhotoStampSyncResult> ProcessAsync(
        string deviceSn,
        string url,
        string rawBody,
        string? flowId,
        CancellationToken cancellationToken)
    {
        var receivedLines = AttendanceLogBodyParser.SplitLines(rawBody);
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var normalizedSerial = deviceSn.Trim().ToUpperInvariant();

        var device = await dbContext.Devices
            .SingleOrDefaultAsync(x => x.SerialNumber == normalizedSerial, cancellationToken);

        if (device is null)
        {
            _logger.LogWarning("Attendance Gateway FLOW DB [{FlowId}] Could not resolve ATTPHOTO device in database. DeviceSn={DeviceSn}", flowId ?? "<none>", normalizedSerial);
            return new AttendancePhotoStampSyncResult(receivedLines.Count, false, null);
        }

        var stamp = HeaderParser.ExtractQueryParam(url, "Stamp");
        if (!string.IsNullOrWhiteSpace(stamp))
        {
            device.AttendancePhotoStamp = stamp.Trim();
            device.UpdatedAtUtc = VietnamTime.Now.DateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Attendance Gateway FLOW DB [{FlowId}] Processed ATTPHOTO payload. DeviceSn={DeviceSn}, ReceivedLines={ReceivedLines}, Stamp={Stamp}",
            flowId ?? "<none>",
            normalizedSerial,
            receivedLines.Count,
            string.IsNullOrWhiteSpace(stamp) ? "<empty>" : stamp);

        return new AttendancePhotoStampSyncResult(receivedLines.Count, true, string.IsNullOrWhiteSpace(stamp) ? null : stamp.Trim());
    }
}

public sealed record AttendancePhotoStampSyncResult(int ReceivedLineCount, bool DeviceResolved, string? Stamp);
