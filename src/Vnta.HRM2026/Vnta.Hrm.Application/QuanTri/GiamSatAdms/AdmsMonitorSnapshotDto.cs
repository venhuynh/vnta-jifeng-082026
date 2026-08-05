namespace Vnta.Hrm.Application.QuanTri.GiamSatAdms;

public sealed record AdmsMonitorSnapshotDto(
    IReadOnlyList<AdmsMonitorDeviceStateDto> Devices,
    IReadOnlyList<AttendanceGatewayRealtimeEventDto> ActivityEvents,
    IReadOnlyList<AttendanceGatewayRealtimeEventDto> RawEvents,
    DateTimeOffset? LastReceivedAtUtc);
