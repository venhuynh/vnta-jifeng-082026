namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Applies allowed manual values to an unlocked hazard allowance snapshot.</summary>
public interface IHazardAllowanceManualAdjustmentService
{
    Task<HazardAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateHazardAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default);
}
