namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public interface IOvertimeRegistrationService
{
    Task<IReadOnlyList<OvertimeRegistrationListItemDto>> SearchAsync(
        OvertimeRegistrationFilter filter,
        CancellationToken cancellationToken = default);

    Task<OvertimeRegistrationDraftDto> CreateDraftAsync(
        CreateOvertimeRegistrationDraftRequest request,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default);

    Task<OvertimeRegistrationListItemDto> SaveAsync(
        UpsertOvertimeRegistrationRequest request,
        bool submitAfterSave,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(
        ChangeOvertimeRegistrationStatusRequest request,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default);
}
