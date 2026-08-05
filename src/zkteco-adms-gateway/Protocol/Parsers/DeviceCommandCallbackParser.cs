namespace Vnta.AttendanceGateway.Protocol.Parsers;

public static class DeviceCommandCallbackParser
{
    public static IReadOnlyList<string> SplitLines(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return Array.Empty<string>();
        }

        return rawBody
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    public static DeviceCommandCallbackLine? ParseLine(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return null;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var segments = rawLine.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            var pair = segment.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2)
            {
                continue;
            }

            values[pair[0]] = pair[1];
        }

        if (!values.TryGetValue("ID", out var idText) || !int.TryParse(idText, out var id))
        {
            return null;
        }

        values.TryGetValue("Return", out var returnCode);
        values.TryGetValue("CMD", out var commandType);

        return new DeviceCommandCallbackLine(id, returnCode, commandType, rawLine);
    }

    public static Dictionary<string, string> ParseInfoBody(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var pair = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2)
            {
                continue;
            }

            var key = NormalizeInfoKey(pair[0]);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = pair[1].Trim();
        }

        return result;
    }

    private static string NormalizeInfoKey(string key)
    {
        return new string(key.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }
}

public sealed record DeviceCommandCallbackLine(int Id, string? ReturnCode, string? CommandType, string RawLine);
