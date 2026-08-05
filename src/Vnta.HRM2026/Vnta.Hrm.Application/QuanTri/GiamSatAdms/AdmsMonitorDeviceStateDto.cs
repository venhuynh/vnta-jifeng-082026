namespace Vnta.Hrm.Application.QuanTri.GiamSatAdms;

public sealed record AdmsMonitorDeviceStateDto(
    string DeviceKey,
    string? DeviceSn,
    string? DeviceName,
    string? LastConnectionId,
    string? LastEventType,
    string? LastSummaryText,
    DateTimeOffset? ConnectionOpenedAtUtc,
    DateTimeOffset? ConnectionClosedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    bool IsOnline);
