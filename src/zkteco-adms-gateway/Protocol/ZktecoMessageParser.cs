using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol;

public static class ZktecoMessageParser
{
    public static List<AttLogDto> ParseAttendanceLogs(string rawBody)
    {
        var logs = new List<AttLogDto>();
        var lines = rawBody.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var parts = line.Split('\t', StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                continue;
            }

            if (!DateTime.TryParse(parts[1], out var tapTime))
            {
                continue;
            }

            tapTime = DateTime.SpecifyKind(tapTime, DateTimeKind.Unspecified);

            logs.Add(new AttLogDto(
                EmployeeCode: ZktecoEmployeeCode.FromPin(parts[0]),
                TapTime: tapTime,
                VerificationMode: int.TryParse(parts[3], out var verificationMode) ? verificationMode : 0,
                InOutMode: int.TryParse(parts[2], out var inOutMode) ? inOutMode : 0));
        }

        return logs;
    }
}
