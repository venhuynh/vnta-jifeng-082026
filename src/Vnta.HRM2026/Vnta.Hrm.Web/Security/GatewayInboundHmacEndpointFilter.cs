using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Vnta.Hrm.Web.Security;

public sealed class GatewayInboundHmacEndpointFilter(
    IOptions<GatewayInboundSecurityOptions> optionsAccessor,
    GatewayInboundReplayStore replayStore,
    ILogger<GatewayInboundHmacEndpointFilter> logger) : IEndpointFilter
{
    private const string KeyIdHeaderName = "X-VNTA-Key-Id";
    private const string TimestampHeaderName = "X-VNTA-Timestamp";
    private const string NonceHeaderName = "X-VNTA-Nonce";
    private const string SignatureHeaderName = "X-VNTA-Signature";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var request = httpContext.Request;
        var options = optionsAccessor.Value;

        if (!TryValidateOptions(options))
        {
            logger.LogError("Gateway inbound security is not configured. Request was rejected.");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var clientCertificate = options.RequireMutualTls
            ? await httpContext.Connection.GetClientCertificateAsync(httpContext.RequestAborted)
            : null;
        if (options.RequireMutualTls && clientCertificate is null)
        {
            logger.LogWarning("Gateway inbound request was rejected because no client certificate was presented.");
            return Results.Unauthorized();
        }

        if (options.RequireMutualTls
            && (options.TrustedClientCertificateSha256Thumbprints.Count == 0
                || !options.TrustedClientCertificateSha256Thumbprints.Contains(
                    Convert.ToHexString(SHA256.HashData(clientCertificate!.RawData)))))
        {
            logger.LogWarning("Gateway inbound request was rejected because its client certificate is not trusted.");
            return Results.Unauthorized();
        }

        if (request.ContentLength is > 0 && request.ContentLength > options.MaxRequestBodyBytes)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        if (!TryGetHeader(request, KeyIdHeaderName, out var keyId)
            || !TryGetHeader(request, TimestampHeaderName, out var timestampValue)
            || !TryGetHeader(request, NonceHeaderName, out var nonce)
            || !TryGetHeader(request, SignatureHeaderName, out var signatureValue)
            || !TryParseTimestamp(timestampValue, options.AllowedClockSkewSeconds, out var timestampUtc)
            || !options.Keys.TryGetValue(keyId, out var sharedSecret)
            || string.IsNullOrWhiteSpace(sharedSecret))
        {
            return Results.Unauthorized();
        }

        byte[] rawBody;
        try
        {
            rawBody = await ReadBodyAsync(request, options.MaxRequestBodyBytes, httpContext.RequestAborted);
        }
        catch (IOException)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        if (!HasValidSignature(request, timestampValue, nonce, rawBody, sharedSecret, signatureValue))
        {
            return Results.Unauthorized();
        }

        var expiresAtUtc = timestampUtc.AddSeconds(options.NonceTtlSeconds);
        if (!replayStore.TryAccept(keyId, nonce, expiresAtUtc))
        {
            logger.LogWarning("Gateway inbound request was rejected because its nonce was already used. KeyId={KeyId}", keyId);
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private static bool TryValidateOptions(GatewayInboundSecurityOptions options) =>
        options.MaxRequestBodyBytes > 0
        && options.AllowedClockSkewSeconds > 0
        && options.NonceTtlSeconds >= options.AllowedClockSkewSeconds
        && options.Keys.Count > 0;

    private static bool TryGetHeader(HttpRequest request, string headerName, out string value)
    {
        value = request.Headers[headerName].ToString().Trim();
        return !string.IsNullOrWhiteSpace(value) && value.Length <= 256;
    }

    private static bool TryParseTimestamp(
        string timestampValue,
        int allowedClockSkewSeconds,
        out DateTimeOffset timestampUtc)
    {
        timestampUtc = default;
        if (!long.TryParse(timestampValue, out var unixTimestamp))
        {
            return false;
        }

        try
        {
            timestampUtc = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        return Math.Abs((DateTimeOffset.UtcNow - timestampUtc).TotalSeconds) <= allowedClockSkewSeconds;
    }

    private static async Task<byte[]> ReadBodyAsync(
        HttpRequest request,
        int maxRequestBodyBytes,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering(bufferThreshold: 30_720, bufferLimit: maxRequestBodyBytes);
        request.Body.Position = 0;
        await using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken);
        request.Body.Position = 0;

        if (buffer.Length > maxRequestBodyBytes)
        {
            throw new IOException("Gateway request body is too large.");
        }

        return buffer.ToArray();
    }

    private static bool HasValidSignature(
        HttpRequest request,
        string timestampValue,
        string nonce,
        byte[] rawBody,
        string sharedSecret,
        string signatureValue)
    {
        byte[] providedSignature;
        try
        {
            providedSignature = Convert.FromHexString(signatureValue);
        }
        catch (FormatException)
        {
            return false;
        }

        var bodyHash = Convert.ToHexString(SHA256.HashData(rawBody));
        var path = $"{request.PathBase}{request.Path}";
        var canonicalRequest = string.Join(
            '\n',
            request.Method.ToUpperInvariant(),
            path,
            timestampValue,
            nonce,
            bodyHash);
        var expectedSignature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(sharedSecret),
            Encoding.UTF8.GetBytes(canonicalRequest));

        return providedSignature.Length == expectedSignature.Length
            && CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature);
    }
}
