using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Vnta.Hrm.Application.QuanTri.MayChamCong;

public static class AttendanceDeviceActivationCode
{
    private const string SecretKey = "VNTA|Attendance Gateway|Activation|2026|InternalOnly";
    private const string Prefix = "VN1";
    private const string Base32Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public const string OptionalActivationCodePattern = @"^$|^\s*(?i:VN1(?:-?[A-HJ-NP-Z2-9]{4}){4})\s*$";

    private static readonly Regex ActivationCodeShapeRegex = new(
        @"^(?i:VN1(?:-?[A-HJ-NP-Z2-9]{4}){4})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Generate(string serialNumber)
    {
        var normalizedSerial = NormalizeSerial(serialNumber);
        if (string.IsNullOrWhiteSpace(normalizedSerial))
        {
            return string.Empty;
        }

        var sourceText = $"VNTA-Attendance Gateway|{Prefix}|{normalizedSerial}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sourceText));
        var shortHash = hash.Take(10).ToArray();
        var encoded = Base32Encode(shortHash);

        return $"{Prefix}-{encoded[..4]}-{encoded.Substring(4, 4)}-{encoded.Substring(8, 4)}-{encoded.Substring(12, 4)}";
    }

    public static bool Validate(string serialNumber, string activationCode)
    {
        if (string.IsNullOrWhiteSpace(serialNumber) || string.IsNullOrWhiteSpace(activationCode))
        {
            return false;
        }

        var expected = NormalizeActivationCode(Generate(serialNumber));
        var actual = NormalizeActivationCode(activationCode);

        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected, actual, StringComparison.Ordinal);
    }

    public static string NormalizeSerial(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return string.Empty;
        }

        return new string(serialNumber
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    public static string NormalizeActivationCode(string activationCode)
    {
        if (string.IsNullOrWhiteSpace(activationCode))
        {
            return string.Empty;
        }

        return new string(activationCode
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    public static bool HasExpectedShape(string? activationCode)
    {
        if (string.IsNullOrWhiteSpace(activationCode))
        {
            return false;
        }

        return ActivationCodeShapeRegex.IsMatch(activationCode.Trim());
    }

    private static string Base32Encode(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var output = new StringBuilder((int)Math.Ceiling(data.Length / 5d) * 8);
        var bitBuffer = 0;
        var bitBufferLength = 0;

        foreach (var value in data)
        {
            bitBuffer = (bitBuffer << 8) | value;
            bitBufferLength += 8;

            while (bitBufferLength >= 5)
            {
                var index = (bitBuffer >> (bitBufferLength - 5)) & 31;
                output.Append(Base32Alphabet[index]);
                bitBufferLength -= 5;
            }
        }

        if (bitBufferLength > 0)
        {
            var index = (bitBuffer << (5 - bitBufferLength)) & 31;
            output.Append(Base32Alphabet[index]);
        }

        return output.ToString();
    }
}
