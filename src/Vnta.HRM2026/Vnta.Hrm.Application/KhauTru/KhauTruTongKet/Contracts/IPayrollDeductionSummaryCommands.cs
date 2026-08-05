namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

/// <summary>Compatibility command facade. Prefer the capability-specific contracts.</summary>
[Obsolete("Inject the capability-specific deduction-summary contracts instead; remove after legacy consumers are retired.")]
public interface IPayrollDeductionSummaryCommands :
    IPayrollDeductionSummarySyncService,
    IPayrollDeductionSummaryRefreshService,
    IPayrollDeductionSummaryManualAdjustmentService,
    IPayrollDeductionSummaryLockService
{
}
