using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Security;

public sealed class DeviceAuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public byte[]? FailureResponse { get; init; }
    public DeviceAuthorizationContext? Device { get; init; }
}
