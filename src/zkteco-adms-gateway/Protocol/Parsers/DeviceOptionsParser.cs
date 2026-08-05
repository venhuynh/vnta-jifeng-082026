using System.Text.RegularExpressions;

namespace Vnta.AttendanceGateway.Protocol.Parsers;

public static class DeviceOptionsParser
{
    public static IReadOnlyDictionary<string, string> Parse(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var normalized = rawBody.Replace("~", string.Empty);
        var tokens = Regex.Split(normalized, @"[\r\n\t]+|,(?=[A-Za-z0-9_]+=)")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = token[..separatorIndex].Trim();
            var value = token[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            values[key.ToUpperInvariant()] = value;
        }

        return values;
    }
}
