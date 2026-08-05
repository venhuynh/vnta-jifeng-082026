namespace Vnta.Hrm.Application.QuanTri.LenhMayChamCong;

public sealed record AdmsDeviceInfoResponseDto(
    int CommandId,
    string DeviceSn,
    DateTime ResponseTime,
    IReadOnlyList<AdmsDeviceInfoItemDto> Items);
