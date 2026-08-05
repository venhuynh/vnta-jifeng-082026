namespace Vnta.AttendanceGateway.Domain;

public sealed class ZktecoDeviceCommand
{
    public int Id { get; set; }

    public string? DeviceSn { get; set; }

    public string? Content { get; set; }

    public DateTime? CommitTime { get; set; }

    public DateTime? TransTime { get; set; }

    public DateTime? ResponseTime { get; set; }

    public string? ReturnValue { get; set; }

    public string? Description { get; set; }
}
