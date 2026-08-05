namespace Vnta.AttendanceGateway.Protocol.Models;

public sealed class ZktecoRequestContext
{
    public required string Method { get; init; }
    public required string Url { get; init; }
    public required string BodyRawText { get; init; }
    public required string ConnectionId { get; init; }
    public required string FlowId { get; init; }
    public DeviceAuthorizationContext? Device { get; init; }
}
