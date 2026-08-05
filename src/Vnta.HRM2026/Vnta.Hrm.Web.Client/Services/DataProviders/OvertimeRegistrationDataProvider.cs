namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class OvertimeRegistrationDataProvider(
    IOvertimeRegistrationService overtimeRegistrationService)
{
    private static readonly OvertimeRegistrationActorContext ClientActorContext =
        new("client-auth-cookie", null, CanManageWorkshopRegistrations: false);

    public Task<IReadOnlyList<OvertimeRegistrationListItemDto>> SearchAsync(
        OvertimeRegistrationFilter filter,
        CancellationToken cancellationToken = default) =>
        overtimeRegistrationService.SearchAsync(filter, cancellationToken);

    public Task<OvertimeRegistrationDraftDto> CreateDraftAsync(
        CreateOvertimeRegistrationDraftRequest request,
        CancellationToken cancellationToken = default) =>
        overtimeRegistrationService.CreateDraftAsync(
            request,
            ClientActorContext,
            cancellationToken);

    public Task<OvertimeRegistrationListItemDto> SaveAsync(
        UpsertOvertimeRegistrationRequest request,
        bool submitAfterSave,
        CancellationToken cancellationToken = default) =>
        overtimeRegistrationService.SaveAsync(
            request,
            submitAfterSave,
            ClientActorContext,
            cancellationToken);

    public Task ChangeStatusAsync(
        ChangeOvertimeRegistrationStatusRequest request,
        CancellationToken cancellationToken = default) =>
        overtimeRegistrationService.ChangeStatusAsync(
            request,
            ClientActorContext,
            cancellationToken);
}
