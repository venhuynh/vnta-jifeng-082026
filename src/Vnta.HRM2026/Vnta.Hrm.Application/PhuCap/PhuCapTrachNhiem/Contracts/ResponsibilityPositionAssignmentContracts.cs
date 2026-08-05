namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

public interface IResponsibilityPositionAssignmentReadService
{
    Task<ResponsibilityPositionAssignmentPageDto> SearchPageAsync(
        ResponsibilityPositionAssignmentQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResponsibilityPositionAssignmentGradeOptionDto>> GetGradeOptionsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}

public interface IResponsibilityPositionAssignmentCommandService
{
    Task<ResponsibilityPositionAssignmentItemDto> SaveAsync(
        SaveResponsibilityPositionAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ResponsibilityPositionAssignmentItemDto> DeactivateAsync(
        DeactivateResponsibilityPositionAssignmentRequest request,
        CancellationToken cancellationToken = default);
}

public interface IResponsibilityPositionAssignmentCopyService
{
    Task<CopyResponsibilityPositionAssignmentsResult> CopyFromPreviousPeriodAsync(
        CopyResponsibilityPositionAssignmentsRequest request,
        CancellationToken cancellationToken = default);
}

public interface IResponsibilityPositionAssignmentExportReadService
{
    Task<IReadOnlyList<ResponsibilityPositionAssignmentExportItemDto>> ExportAllAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}

public sealed record ResponsibilityPositionAssignmentQuery(
    int Year,
    int Month,
    string? SearchText,
    int Skip = 0,
    int Take = 100);

public sealed record ResponsibilityPositionAssignmentPageDto(
    IReadOnlyList<ResponsibilityPositionAssignmentItemDto> Rows,
    int TotalCount);

public sealed record ResponsibilityPositionAssignmentItemDto(
    Guid Id,
    int Year,
    int Month,
    Guid GradeId,
    string GradeCode,
    string GradeName,
    Guid PositionId,
    string PositionCode,
    string PositionName,
    bool IsActive,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record ResponsibilityPositionAssignmentGradeOptionDto(
    Guid Id,
    int Year,
    int Month,
    string Code,
    string Name,
    decimal StandardResponsibilityAllowanceAmount,
    int DisplayOrder,
    bool IsActive,
    string? Note);

public sealed record SaveResponsibilityPositionAssignmentRequest(
    Guid? Id,
    int Year,
    int Month,
    Guid GradeId,
    Guid PositionId,
    bool IsActive,
    string? Note,
    DateTime? OriginalUpdatedAtUtc);

public sealed record DeactivateResponsibilityPositionAssignmentRequest(
    Guid Id,
    int Year,
    int Month,
    DateTime? OriginalUpdatedAtUtc);

public sealed record CopyResponsibilityPositionAssignmentsRequest(
    int Year,
    int Month);

public sealed record CopyResponsibilityPositionAssignmentsResult(
    int Year,
    int Month,
    int PreviousYear,
    int PreviousMonth,
    int SourceCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedMissingGradeCount);

public sealed record ResponsibilityPositionAssignmentExportItemDto(
    Guid Id,
    int Year,
    int Month,
    string PositionCode,
    string PositionName,
    string GradeCode,
    string GradeName,
    string Status,
    string? Note);

public sealed class ResponsibilityPositionAssignmentConflictException(string message)
    : InvalidOperationException(message);

public sealed class ResponsibilityAllowanceConflictException(string message)
    : InvalidOperationException(message);
