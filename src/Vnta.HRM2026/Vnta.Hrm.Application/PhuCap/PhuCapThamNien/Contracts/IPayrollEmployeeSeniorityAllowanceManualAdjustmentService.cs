namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public interface IPayrollEmployeeSeniorityAllowanceManualAdjustmentService
{
    Task<PayrollEmployeeSeniorityAllowanceListItemDto> UpdateManualValuesAsync(
        UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default);
}
