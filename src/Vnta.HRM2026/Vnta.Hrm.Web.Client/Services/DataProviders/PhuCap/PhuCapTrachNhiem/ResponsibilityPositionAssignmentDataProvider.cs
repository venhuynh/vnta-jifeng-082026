using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Interactive Server adapter for the responsibility-position assignment screen.
/// It keeps UI models isolated from the specialised Application contracts and
/// ensures every write is executed inside an interactive audit scope.
/// </summary>
public sealed class ResponsibilityPositionAssignmentDataProvider(
    IResponsibilityPositionAssignmentReadService readService,
    IResponsibilityPositionAssignmentCommandService commandService,
    IResponsibilityPositionAssignmentCopyService copyService,
    IResponsibilityPositionAssignmentExportReadService exportReadService,
    IPayrollAdministrationAuthorizer payrollAdministrationAuthorizer,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory)
{
    public async Task<ResponsibilityPositionAssignmentScreenData> LoadAsync(
        int year,
        int month,
        string? searchText,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Both read contracts are backed by the same scoped DbContext. Keep the
        // operations sequential because EF Core does not permit concurrent use of
        // one context within an Interactive Server circuit.
        var gradeOptions = await readService.GetGradeOptionsAsync(year, month, cancellationToken);
        var page = await readService.SearchPageAsync(
            new ResponsibilityPositionAssignmentQuery(year, month, searchText, skip, take),
            cancellationToken);

        var grades = gradeOptions
            .Select(MapGrade)
            .ToArray();
        return new ResponsibilityPositionAssignmentScreenData(
            grades,
            page.Rows.Select(MapMapping).ToArray(),
            page.TotalCount);
    }

    public async Task<PayrollResponsibilityAllowanceGradePositionDto> SaveAsync(
        SaveResponsibilityPositionAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var action = request.Id.HasValue && request.Id.Value != Guid.Empty
            ? AuditActions.ResponsibilityPositionAssignment.Update
            : AuditActions.ResponsibilityPositionAssignment.Create;
        var result = await auditCommandScopeFactory.ExecuteAsync(
            action,
            token => commandService.SaveAsync(request, token),
            cancellationToken: cancellationToken);
        return MapMapping(result);
    }

    public async Task<PayrollResponsibilityAllowanceGradePositionDto> DeactivateAsync(
        DeactivateResponsibilityPositionAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.ResponsibilityPositionAssignment.Deactivate,
            token => commandService.DeactivateAsync(request, token),
            cancellationToken: cancellationToken);
        return MapMapping(result);
    }

    public async Task<CopyResponsibilityPositionAssignmentsResult> CopyFromPreviousPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.ResponsibilityPositionAssignment.CopyFromPreviousPeriod,
            token => copyService.CopyFromPreviousPeriodAsync(
                new CopyResponsibilityPositionAssignmentsRequest(year, month),
                token),
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<ResponsibilityPositionAssignmentExportItemDto>> LoadAllForExportAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        exportReadService.ExportAllAsync(year, month, cancellationToken);

    private static PayrollResponsibilityAllowanceGradeDto MapGrade(
        ResponsibilityPositionAssignmentGradeOptionDto source) =>
        new(
            source.Id,
            source.Year,
            source.Month,
            source.Code,
            source.Name,
            source.StandardResponsibilityAllowanceAmount,
            source.DisplayOrder,
            source.IsActive,
            source.Note);

    private static PayrollResponsibilityAllowanceGradePositionDto MapMapping(
        ResponsibilityPositionAssignmentItemDto source) =>
        new(
            source.Id,
            source.Year,
            source.Month,
            source.GradeId,
            source.PositionId,
            source.PositionCode,
            source.PositionName,
            source.IsActive,
            source.Note,
            source.CreatedAtUtc,
            source.UpdatedAtUtc);
}

public sealed record ResponsibilityPositionAssignmentScreenData(
    IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> Grades,
    IReadOnlyList<PayrollResponsibilityAllowanceGradePositionDto> Mappings,
    int TotalCount);
