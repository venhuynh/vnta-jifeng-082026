namespace Vnta.AttendanceGateway.Configuration;

public sealed class CoreApiOptions
{
    public const string SectionName = "CoreApi";

    public bool Enabled { get; set; } = false;

    public string BaseUrl { get; set; } = string.Empty;

    public string AttendanceEndpoint { get; set; } = "/api/integration/attendance-gateway/attendance";

    public string SystemLogEndpoint { get; set; } = "/api/integration/attendance-gateway/system-logs";

    public string RealtimeEndpoint { get; set; } = "/api/integration/adms/realtime/events";

    public int TimeoutSeconds { get; set; } = 5;

    public string GatewayKeyId { get; set; } = string.Empty;

    public string GatewayHmacSecret { get; set; } = string.Empty;

    public string ClientCertificatePath { get; set; } = string.Empty;

    public string ClientCertificatePassword { get; set; } = string.Empty;

    public string TrustedServerCertificateSha256Thumbprint { get; set; } = string.Empty;
}
