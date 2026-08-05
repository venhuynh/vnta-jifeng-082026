namespace Vnta.Hrm.Application.QuanTri.LenhMayChamCong;

public sealed record AdmsDeviceCommandLookupOptionsDto(
    IReadOnlyList<AdmsLookupItemDto> Statuses);
