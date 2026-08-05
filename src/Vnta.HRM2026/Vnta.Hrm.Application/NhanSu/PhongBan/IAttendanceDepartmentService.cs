namespace Vnta.Hrm.Application.NhanSu.PhongBan;

public interface IAttendanceDepartmentService
{
    Task<IReadOnlyList<AttendanceDepartmentDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<string?> ValidateAsync(
        UpsertAttendanceDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendanceDepartmentDto> SaveAsync(
        UpsertAttendanceDepartmentRequest request,
        bool isNew,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
