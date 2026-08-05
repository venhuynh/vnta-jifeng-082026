namespace Vnta.Hrm.Application.QuanTri.LenhMayChamCong;

public static class AdmsDeviceInfoResponseParser
{
    public static IReadOnlyList<AdmsDeviceInfoItemDto> Parse(string? returnValue)
    {
        if (string.IsNullOrWhiteSpace(returnValue))
        {
            return [];
        }

        var rows = new List<AdmsDeviceInfoItemDto>();
        var indexesByNormalizedKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in SplitLines(returnValue))
        {
            if (IsInfoCallbackMetadata(line))
            {
                continue;
            }

            var pair = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2 || string.IsNullOrWhiteSpace(pair[0]))
            {
                continue;
            }

            var key = pair[0].Trim();
            var normalizedKey = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                continue;
            }

            var item = new AdmsDeviceInfoItemDto(key, normalizedKey, pair[1].Trim());
            if (indexesByNormalizedKey.TryGetValue(normalizedKey, out var existingIndex))
            {
                rows[existingIndex] = item;
                continue;
            }

            indexesByNormalizedKey[normalizedKey] = rows.Count;
            rows.Add(item);
        }

        return rows;
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        return value.Split(
            ["\r\n", "\n"],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsInfoCallbackMetadata(string line)
    {
        return line.Contains("ID=", StringComparison.OrdinalIgnoreCase)
            && line.Contains("Return=", StringComparison.OrdinalIgnoreCase)
            && line.Contains("CMD=INFO", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeKey(string key)
    {
        return new string(key
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
