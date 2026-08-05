namespace Vnta.Hrm.Application.KhauTru.KhauTruKhac;

public interface IPayrollEmployeeOtherDeductionAllowanceService
{
    Task PreparePeriodAsync(int year, int month, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayrollEmployeeOtherDeductionAllowanceListItemDto>> SearchAsync(
        PayrollEmployeeOtherDeductionAllowanceFilter filter,
        CancellationToken cancellationToken = default);

    Task<PayrollEmployeeOtherDeductionAllowancePageDto> SearchPageAsync(
        PayrollEmployeeOtherDeductionAllowanceFilter filter,
        CancellationToken cancellationToken = default);

    Task<RefreshPayrollEmployeeOtherDeductionAllowanceResult> RefreshAsync(
        RefreshPayrollEmployeeOtherDeductionAllowanceRequest request,
        CancellationToken cancellationToken = default);

    Task<PayrollEmployeeOtherDeductionAllowanceListItemDto> UpdateManualValuesAsync(
        UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default);

    Task<PayrollEmployeeOtherDeductionAllowanceListItemDto> SetLockStateAsync(
        SetPayrollEmployeeOtherDeductionAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
