namespace Vnta.AttendanceGateway.Configuration;

public sealed class AttendanceGatewayOptions
{
    public const int DefaultRealtimeRawBodyMaxLength = 4096;

    public const string SectionName = "AttendanceGateway";

    public int ListenerPort { get; set; } = 8080;

    public int ControlPlanePort { get; set; } = 5005;

    public bool AutoStartTcpListener { get; set; } = true;

    public bool RawCommunicationLogEnabled { get; set; } = true;

    public string RawCommunicationLogPath { get; set; } = "Logs/jifeng-attendance-gateway-raw";

    public int RealtimeRawBodyMaxLength { get; set; } = DefaultRealtimeRawBodyMaxLength;

    public int RealtimeForwardQueueCapacity { get; set; } = 2048;
}


