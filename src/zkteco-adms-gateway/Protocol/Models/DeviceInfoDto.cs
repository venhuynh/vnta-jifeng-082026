namespace Vnta.AttendanceGateway.Protocol.Models;

public sealed record DeviceInfoDto(
    string FirmwareVersion,
    int PushMode,
    int Language,
    int Charset,
    string IpAddress,
    int? TransactionInterval,
    int? Delay,
    int? TimeZoneOffset,
    int? Realtime,
    string RawInfo);
