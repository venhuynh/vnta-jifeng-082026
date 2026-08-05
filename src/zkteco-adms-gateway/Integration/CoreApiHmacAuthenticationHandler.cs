using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Vnta.AttendanceGateway.Configuration;
using Microsoft.Extensions.Options;

namespace Vnta.AttendanceGateway.Integration;

/// <summary>
/// Signs every gateway-to-HRM request so the inbound HRM contract can validate
/// the caller and reject replays. The secret is supplied only through runtime configuration.
/// </summary>
public sealed class CoreApiHmacAuthenticationHandler(IOptions<CoreApiOptions> options) : DelegatingHandler
{
    private readonly CoreApiOptions _options = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.GatewayKeyId) || string.IsNullOrWhiteSpace(_options.GatewayHmacSecret))
        {
            throw new InvalidOperationException("Core API HMAC credentials are not configured.");
        }

        if (request.RequestUri is null)
        {
            throw new InvalidOperationException("Core API request URI is required for HMAC signing.");
        }

        var body = request.Content is null
            ? Array.Empty<byte>()
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var nonce = Guid.NewGuid().ToString("N");
        var bodyHash = Convert.ToHexString(SHA256.HashData(body));
        var path = request.RequestUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
        var canonicalRequest = string.Join(
            "\n",
            request.Method.Method.ToUpperInvariant(),
            "/" + path,
            timestamp,
            nonce,
            bodyHash);
        var signature = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.GatewayHmacSecret),
            Encoding.UTF8.GetBytes(canonicalRequest)));

        request.Headers.Remove("X-VNTA-Key-Id");
        request.Headers.Remove("X-VNTA-Timestamp");
        request.Headers.Remove("X-VNTA-Nonce");
        request.Headers.Remove("X-VNTA-Signature");
        request.Headers.TryAddWithoutValidation("X-VNTA-Key-Id", _options.GatewayKeyId);
        request.Headers.TryAddWithoutValidation("X-VNTA-Timestamp", timestamp);
        request.Headers.TryAddWithoutValidation("X-VNTA-Nonce", nonce);
        request.Headers.TryAddWithoutValidation("X-VNTA-Signature", signature);

        return await base.SendAsync(request, cancellationToken);
    }
}
