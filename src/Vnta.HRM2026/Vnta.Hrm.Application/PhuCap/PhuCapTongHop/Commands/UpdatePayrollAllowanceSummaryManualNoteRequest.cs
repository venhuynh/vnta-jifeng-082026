namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>
/// Command used inside the application to change the only summary-owned manual field.
/// Allowance amounts are projections calculated by their source features and are deliberately absent.
/// </summary>
public sealed record UpdatePayrollAllowanceSummaryManualNoteRequest(
    Guid Id,
    string? Note,
    DateTime? OriginalUpdatedAtUtc,
    string? Actor);
