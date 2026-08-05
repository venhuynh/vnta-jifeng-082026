using System.Text.Json.Serialization;

namespace Vnta.AttendanceGateway.Protocol.Models;

public sealed class DeviceCommandDto
{
    public string? CommandId { get; init; }

    public string? Command { get; init; }

    public string? Payload { get; init; }

    [JsonIgnore]
    public bool HasContent => !string.IsNullOrWhiteSpace(Command) || !string.IsNullOrWhiteSpace(Payload);

    public string ToDeviceResponse()
    {
        if (!string.IsNullOrWhiteSpace(Payload))
        {
            return Payload!;
        }

        if (!string.IsNullOrWhiteSpace(Command))
        {
            return Command!;
        }

        return "OK: 0";
    }
}
