using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Vnta.AttendanceGateway.Configuration;
using Vnta.AttendanceGateway.Integration.Models;

namespace Vnta.AttendanceGateway.Integration;

public sealed class AdmsRealtimeEventQueue
{
    private readonly Channel<CoreApiAdmsRealtimeEventRequest> _channel;
    private readonly ILogger<AdmsRealtimeEventQueue> _logger;

    public AdmsRealtimeEventQueue(
        IOptions<AttendanceGatewayOptions> options,
        ILogger<AdmsRealtimeEventQueue> logger)
    {
        _logger = logger;

        var capacity = Math.Max(1, options.Value.RealtimeForwardQueueCapacity);
        var channelOptions = new BoundedChannelOptions(capacity) {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        _channel = Channel.CreateBounded<CoreApiAdmsRealtimeEventRequest>(channelOptions);
    }

    public bool TryEnqueue(CoreApiAdmsRealtimeEventRequest payload)
    {
        var queued = _channel.Writer.TryWrite(payload);
        if(!queued) {
            _logger.LogWarning(
                "Dropped realtime ADMS event because queue is full. EventType={EventType}, DeviceSn={DeviceSn}, FlowId={FlowId}",
                payload.EventType,
                payload.Sn ?? "<none>",
                payload.FlowId ?? "<none>");
        }

        return queued;
    }

    public IAsyncEnumerable<CoreApiAdmsRealtimeEventRequest> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
