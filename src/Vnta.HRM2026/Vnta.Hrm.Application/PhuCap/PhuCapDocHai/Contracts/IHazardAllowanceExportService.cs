namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Exports the complete server-filtered hazard allowance dataset.</summary>
public interface IHazardAllowanceExportService
{
    Task<IReadOnlyList<HazardAllowanceListItemDto>> ExportAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default);
}
