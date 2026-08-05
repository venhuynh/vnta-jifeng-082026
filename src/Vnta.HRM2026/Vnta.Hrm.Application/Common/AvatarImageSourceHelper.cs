using System.Text;

namespace Vnta.Hrm.Application.Common;

public static class AvatarImageSourceHelper
{
    private const string DefaultContentType = "image/jpeg";

    public static string? NormalizeSource(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (IsImmediateDirectSource(trimmed))
        {
            return trimmed;
        }

        var compactValue = RemoveWhitespace(trimmed);
        if (compactValue.Length == 0)
        {
            return null;
        }

        if (TryDecodeHexPayload(compactValue, out var hexBytes))
        {
            return BuildDataUrl(hexBytes);
        }

        if (TryDecodeBase64Payload(compactValue, out var base64Bytes))
        {
            return BuildDataUrl(base64Bytes);
        }

        if (LooksLikePathSource(trimmed))
        {
            return trimmed;
        }

        return null;
    }

    public static string? BuildDataUrl(byte[]? bytes, string? contentType = null)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? DetectImageContentType(bytes)
            : contentType.Trim();

        return $"data:{normalizedContentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static bool IsImmediateDirectSource(string value)
        => value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("images/", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("./", StringComparison.Ordinal)
            || value.StartsWith("../", StringComparison.Ordinal);

    private static bool LooksLikePathSource(string value)
        => value.StartsWith("/", StringComparison.Ordinal);

    private static string RemoveWhitespace(string value)
        => new(value.Where(static ch => !char.IsWhiteSpace(ch)).ToArray());

    private static bool TryDecodeHexPayload(string value, out byte[] bytes)
    {
        bytes = [];

        var normalizedHex = value.StartsWith("\\x", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value[2..]
            : null;

        if (string.IsNullOrWhiteSpace(normalizedHex)
            || normalizedHex.Length % 2 != 0
            || normalizedHex.Any(static ch => !Uri.IsHexDigit(ch)))
        {
            return false;
        }

        try
        {
            bytes = Convert.FromHexString(normalizedHex);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryDecodeBase64Payload(string value, out byte[] bytes)
    {
        bytes = [];

        if (value.Length < 16 || value.Length % 4 != 0)
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (!(char.IsLetterOrDigit(ch) || ch is '+' or '/' or '='))
            {
                return false;
            }
        }

        try
        {
            bytes = Convert.FromBase64String(value);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string DetectImageContentType(byte[] bytes)
    {
        if (IsJpeg(bytes))
        {
            return "image/jpeg";
        }

        if (IsPng(bytes))
        {
            return "image/png";
        }

        if (IsGif(bytes))
        {
            return "image/gif";
        }

        if (IsWebp(bytes))
        {
            return "image/webp";
        }

        if (IsSvg(bytes))
        {
            return "image/svg+xml";
        }

        return DefaultContentType;
    }

    private static bool IsJpeg(IReadOnlyList<byte> bytes)
        => bytes.Count >= 3
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[2] == 0xFF;

    private static bool IsPng(IReadOnlyList<byte> bytes)
        => bytes.Count >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A;

    private static bool IsGif(IReadOnlyList<byte> bytes)
        => bytes.Count >= 4
            && bytes[0] == 0x47
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x38;

    private static bool IsWebp(IReadOnlyList<byte> bytes)
        => bytes.Count >= 12
            && bytes[0] == 0x52
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x46
            && bytes[8] == 0x57
            && bytes[9] == 0x45
            && bytes[10] == 0x42
            && bytes[11] == 0x50;

    private static bool IsSvg(byte[] bytes)
    {
        var previewLength = Math.Min(bytes.Length, 256);
        var preview = Encoding.UTF8
            .GetString(bytes, 0, previewLength)
            .TrimStart('\uFEFF', ' ', '\t', '\r', '\n');

        return preview.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || (preview.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
                && preview.Contains("<svg", StringComparison.OrdinalIgnoreCase));
    }
}
