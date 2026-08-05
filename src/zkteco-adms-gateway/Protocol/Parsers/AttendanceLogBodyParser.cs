using System;

namespace Vnta.AttendanceGateway.Protocol.Parsers;

public static class AttendanceLogBodyParser
{
    public static IReadOnlyList<string> SplitLines(string rawBody)
    {
        return rawBody
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    public static AttendanceLogLine? ParseLine(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return null;
        }

        var parts = rawLine
            .Split('\t')
            .Select(x => x.Trim())
            .ToArray();

        if (parts.Length < 4)
        {
            return null;
        }

        if (!DateTime.TryParse(parts[1], out var attTime))
        {
            return null;
        }

        attTime = DateTime.SpecifyKind(attTime, DateTimeKind.Unspecified);

        int? maskFlag = null;
        if (parts.Length > 7 && int.TryParse(parts[7], out var parsedMaskFlag))
        {
            maskFlag = parsedMaskFlag;
        }

        return new AttendanceLogLine(
            rawLine.Trim(),
            parts[0],
            attTime,
            parts[2],
            parts[3],
            parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]) ? parts[4] : "0",
            parts.Length > 5 ? parts[5] : null,
            parts.Length > 6 ? parts[6] : null,
            maskFlag,
            parts.Length > 8 ? parts[8] : null);
    }
}

public sealed record AttendanceLogLine(
    string RawLine,
    string Pin,
    DateTime AttTime,
    string Status,
    string Verify,
    string WorkCode,
    string? Reserved1,
    string? Reserved2,
    int? MaskFlag,
    string? Temperature);
