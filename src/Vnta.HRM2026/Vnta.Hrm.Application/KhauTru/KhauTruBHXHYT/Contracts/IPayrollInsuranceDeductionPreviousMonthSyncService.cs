namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public interface IPayrollInsuranceDeductionPreviousMonthSyncService
{
    Task<SyncPayrollInsuranceDeductionFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollInsuranceDeductionFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default);
}
