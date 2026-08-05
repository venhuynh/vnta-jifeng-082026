namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

/// <summary>
/// Compatibility contract for consumers compiled against the original command surface.
/// New consumers should depend on the capability contract for their use case.
/// </summary>
[Obsolete("Inject a capability-specific union-fee deduction contract instead.")]
public interface IPayrollUnionFeeDeductionCommandService :
    IPayrollUnionFeeDeductionPeriodPreparationService,
    IPayrollUnionFeeDeductionRefreshService,
    IPayrollUnionFeeDeductionManualAdjustmentService,
    IPayrollUnionFeeDeductionLockService
{
}
