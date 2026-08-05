using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Parsers;

public static class DeviceInfoParser
{
    public static DeviceInfoDto? Parse(string rawInfo)
    {
        if (string.IsNullOrWhiteSpace(rawInfo))
        {
            return null;
        }

        var parts = rawInfo.Split(',', StringSplitOptions.TrimEntries);
        return new DeviceInfoDto(
            FirmwareVersion: GetString(parts, 0),
            PushMode: GetInt(parts, 1) ?? 0,
            Language: GetInt(parts, 2) ?? 0,
            Charset: GetInt(parts, 3) ?? 0,
            IpAddress: GetString(parts, 4),
            TransactionInterval: GetInt(parts, 5),
            Delay: GetInt(parts, 6),
            TimeZoneOffset: GetInt(parts, 7),
            Realtime: GetInt(parts, 8),
            RawInfo: rawInfo);
    }

    private static string GetString(string[] parts, int index)
    {
        return index < parts.Length ? parts[index] : string.Empty;
    }

    private static int? GetInt(string[] parts, int index)
    {
        if (index >= parts.Length)
        {
            return null;
        }

        return int.TryParse(parts[index], out var value) ? value : null;
    }
}
