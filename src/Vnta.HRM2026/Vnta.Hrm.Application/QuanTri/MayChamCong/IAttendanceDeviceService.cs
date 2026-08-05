namespace Vnta.Hrm.Application.QuanTri.MayChamCong;

public interface IAttendanceDeviceService
{
    Task<IReadOnlyList<AttendanceDeviceDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<string?> ValidateAsync(
        UpsertAttendanceDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendanceDeviceDto> SaveAsync(
        UpsertAttendanceDeviceRequest request,
        bool isNew,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
