namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public interface IPayrollEmployeeSeniorityAllowanceLockService
{
    Task<PayrollEmployeeSeniorityAllowanceListItemDto> SetLockStateAsync(
        SetPayrollEmployeeSeniorityAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
