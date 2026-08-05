namespace Vnta.Hrm.Application.CaKip.CaiDatXepCa;

public interface IShiftSchedulingSettingService
{
    Task<IReadOnlyList<ShiftSchedulingSettingListItemDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<string?> ValidateAsync(
        UpsertShiftSchedulingSettingRequest request,
        CancellationToken cancellationToken = default);

    Task<ShiftSchedulingSettingListItemDto> SaveAsync(
        UpsertShiftSchedulingSettingRequest request,
        bool isNew,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
