namespace Vnta.Hrm.Application.NhanSu.ChucVu;

public interface IAttendancePositionService
{
    Task<IReadOnlyList<AttendancePositionListItemDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<string?> ValidateAsync(
        UpsertAttendancePositionRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendancePositionListItemDto> SaveAsync(
        UpsertAttendancePositionRequest request,
        bool isNew,
        CancellationToken cancellationToken = default);

    Task RefreshEmployeeCountsAsync(CancellationToken cancellationToken = default);
}
