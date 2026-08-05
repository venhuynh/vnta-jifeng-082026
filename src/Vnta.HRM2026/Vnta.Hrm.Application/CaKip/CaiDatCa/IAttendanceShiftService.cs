namespace Vnta.Hrm.Application.CaKip.CaiDatCa;

public interface IAttendanceShiftService
{
    Task<IReadOnlyList<AttendanceShiftListItemDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<string?> ValidateAsync(
        UpsertAttendanceShiftRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendanceShiftListItemDto> SaveAsync(
        UpsertAttendanceShiftRequest request,
        bool isNew,
        CancellationToken cancellationToken = default);
}
