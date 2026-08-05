namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

/// <summary>EF adapter for the export capability only.</summary>
public sealed class DatabaseHazardAllowanceExportService(HazardAllowanceReadProjection projection)
    : IHazardAllowanceExportService
{
    public Task<IReadOnlyList<HazardAllowanceListItemDto>> ExportAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        projection.ExportAsync(filter, cancellationToken);
}
