using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT.Models;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruBHXHYT;

public interface IPayrollInsuranceDeductionDataProvider
{
    Task<PayrollInsuranceDeductionLoadResult> SearchAsync(PayrollInsuranceDeductionFilter filter, CancellationToken cancellationToken = default);
    Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(int targetPayrollMonth, int targetPayrollYear, CancellationToken cancellationToken = default);
    Task<RefreshPayrollInsuranceDeductionResult> RefreshRowAsync(int targetPayrollMonth, int targetPayrollYear, Guid payrollDeductionSummaryRecordId, CancellationToken cancellationToken = default);
    Task<MonthlyWorkSummaryGridRowRecord?> LoadEmployeeMonthlyWorkAsync(Guid insuranceDeductionRecordId, Guid payrollDeductionSummaryRecordId, Guid employeeId, int payrollYear, int payrollMonth, CancellationToken cancellationToken = default);
    Task<PayrollInsuranceDeductionRecord> UpdateManualValuesAsync(UpdatePayrollInsuranceDeductionManualValuesRequest request, CancellationToken cancellationToken = default);
    Task<PayrollInsuranceDeductionRecord> SetLockStateAsync(Guid payrollDeductionSummaryRecordId, bool isLocked, DateTime originalUpdatedAtUtc, CancellationToken cancellationToken = default);
    Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(SetPayrollInsuranceDeductionBatchLockStateRequest request, CancellationToken cancellationToken = default);
}
