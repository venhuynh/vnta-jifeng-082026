namespace Vnta.AttendanceGateway.Configuration;

public sealed class FrontendCorsOptions
{
    public const string SectionName = "FrontendCors";

    public string[] AllowedOrigins { get; init; } = [];

    public bool AllowPrivateNetworkOrigins { get; init; } = true;
}
