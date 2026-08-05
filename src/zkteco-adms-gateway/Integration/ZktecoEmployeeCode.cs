namespace Vnta.AttendanceGateway.Integration;

internal static class ZktecoEmployeeCode
{
    public static string FromPin(string pin)
    {
        var normalized = pin.Trim().ToUpperInvariant();
        if (normalized.Length < 5 && normalized.All(char.IsDigit))
        {
            return normalized.PadLeft(5, '0');
        }

        return normalized;
    }
}
