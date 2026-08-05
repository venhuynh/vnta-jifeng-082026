namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

/// <summary>Read-only EF boundary for hazard allowance queries.</summary>
public sealed class DatabaseHazardAllowanceReadService(HazardAllowanceReadProjection projection)
    : IHazardAllowanceReadService
{
    public Task<IReadOnlyList<HazardAllowanceListItemDto>> SearchAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default) =>
        projection.SearchAsync(filter, cancellationToken);

    public Task<HazardAllowancePageDto> SearchPageAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default) =>
        projection.SearchPageAsync(filter, cancellationToken);

    public Task<HazardAllowanceSummaryDto> GetSummaryAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default) =>
        projection.GetSummaryAsync(filter, cancellationToken);

}
