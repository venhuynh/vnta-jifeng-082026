namespace Vnta.Hrm.Application.ChamCong.CodeKetQuaTinhCong;

public interface IAttendanceStatusCodeService
{
    Task<IReadOnlyList<AttendanceStatusCodeListItemDto>> GetAsync(
        CancellationToken cancellationToken = default);

    Task<AttendanceStatusCodeListItemDto> UpdateFlagsAsync(
        UpdateAttendanceStatusCodeFlagsRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
