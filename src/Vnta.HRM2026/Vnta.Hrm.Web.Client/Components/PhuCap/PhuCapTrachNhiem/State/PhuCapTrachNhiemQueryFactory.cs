using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Builds the server query from an immutable UI snapshot. This keeps query
/// construction independently testable and outside reload orchestration.
/// </summary>
internal interface IPhuCapTrachNhiemQueryFactory
{
    PayrollResponsibilityAllowanceAbcQuery Create(PhuCapTrachNhiemReloadSnapshot snapshot);
}

internal sealed class PhuCapTrachNhiemQueryFactory : IPhuCapTrachNhiemQueryFactory
{
    public PayrollResponsibilityAllowanceAbcQuery Create(PhuCapTrachNhiemReloadSnapshot snapshot) =>
        new(
            snapshot.Year,
            snapshot.Month,
            snapshot.SearchText,
            snapshot.SummaryBadgeKey,
            snapshot.PageIndex * snapshot.PageSize,
            snapshot.PageSize);
}

internal readonly record struct PhuCapTrachNhiemReloadSnapshot(
    int Year,
    int Month,
    string? SearchText,
    string? SummaryBadgeKey,
    int PageIndex,
    int PageSize);
