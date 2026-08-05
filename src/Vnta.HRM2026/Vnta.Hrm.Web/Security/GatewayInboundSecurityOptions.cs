namespace Vnta.Hrm.Web.Security;

public sealed class GatewayInboundSecurityOptions
{
    public const string SectionName = "IntegrationSecurity:GatewayInbound";

    public bool RequireMutualTls { get; set; } = true;

    public int AllowedClockSkewSeconds { get; set; } = 300;

    public int NonceTtlSeconds { get; set; } = 600;

    public int MaxRequestBodyBytes { get; set; } = 1_048_576;

    public Dictionary<string, string> Keys { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// SHA-256 hashes of raw certificates explicitly trusted for the attendance gateway client.
    /// </summary>
    public HashSet<string> TrustedClientCertificateSha256Thumbprints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
