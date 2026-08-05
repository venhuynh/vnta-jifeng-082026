using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public sealed class BioDataHandler : IRequestHandler
{
    private readonly BioDataSyncService _bioDataSyncService;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly ILogger<BioDataHandler> _logger;

    public bool RequiresDeviceAuthorization => true;

    public BioDataHandler(
        BioDataSyncService bioDataSyncService,
        AdmsActivityPublisher admsActivityPublisher,
        ILogger<BioDataHandler> logger)
    {
        _bioDataSyncService = bioDataSyncService;
        _admsActivityPublisher = admsActivityPublisher;
        _logger = logger;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "POST"
               && url.Contains("/iclock/cdata", StringComparison.OrdinalIgnoreCase)
               && url.Contains("table=BIODATA", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var sn = requestContext.Device?.SerialNumber ?? string.Empty;
        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Processing BIODATA payload. SN={SN}", requestContext.FlowId, sn);

        var result = await _bioDataSyncService.ProcessAsync(
            sn,
            requestContext.Url,
            requestContext.BodyRawText,
            requestContext.FlowId,
            cancellationToken);

        _logger.LogInformation(
            "VNTA Attendance Gateway FLOW HANDLER [{FlowId}] BIODATA response prepared. SN={SN}, ReceivedLines={ReceivedLines}, SavedLines={SavedLines}, Stamp={Stamp}",
            requestContext.FlowId,
            sn,
            result.ReceivedLineCount,
            result.SavedLineCount,
            result.Stamp ?? "<empty>");

        await _admsActivityPublisher.PublishAsync(
            sn,
            null,
            requestContext.Method,
            requestContext.Url,
            "biodata-processed",
            result.DeviceResolved ? "processed" : "ignored",
            result.DeviceResolved
                ? $"Đã xử lý BIODATA. Nhận {result.ReceivedLineCount} dòng, lưu {result.SavedLineCount} dòng."
                : "Bỏ qua BIODATA vì không tìm thấy thiết bị tương ứng.",
            requestContext.BodyRawText,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken);

        return ZktecoResponseBuilder.BuildHttpResponse($"OK:{result.ReceivedLineCount}");
    }
}
