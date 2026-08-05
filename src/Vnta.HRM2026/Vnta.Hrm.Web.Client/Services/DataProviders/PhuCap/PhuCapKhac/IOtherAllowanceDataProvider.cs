using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapKhac;

public interface IOtherAllowanceReadDataProvider
{
    Task<OtherAllowancePageDto> SearchPageAsync(OtherAllowanceFilter filter, CancellationToken cancellationToken = default);
}

public interface IOtherAllowanceCreateDataProvider
{
    Task<PayrollAllowanceSummaryLoadResult> SearchCreateEmployeesAsync(int payrollMonth, int payrollYear, int take, CancellationToken cancellationToken = default);
    Task<OtherAllowanceListItemDto> CreateAsync(Guid payrollAllowanceSummaryRecordId, string allowanceName, bool isFixedAmount, decimal allowanceAmount, string? note, CancellationToken cancellationToken = default);
}

public interface IOtherAllowancePreviousMonthSyncDataProvider
{
    Task<SyncOtherAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        CancellationToken cancellationToken = default);
}

public interface IOtherAllowanceUpdateDataProvider
{
    Task<OtherAllowanceListItemDto> UpdateAsync(Guid id, string allowanceName, bool isFixedAmount, decimal allowanceAmount, string? note, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default);
}

public interface IOtherAllowanceLockDataProvider
{
    Task SetLockStateAsync(Guid id, bool isLocked, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default);
    Task<SetOtherAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        int payrollMonth,
        int payrollYear,
        bool isLocked,
        IEnumerable<OtherAllowanceListItemDto>? rows = null,
        CancellationToken cancellationToken = default);
}

public interface IOtherAllowanceMonthlyWorkDataProvider
{
    Task<MonthlyWorkSummaryGridRowRecord?> LoadEmployeeMonthlyWorkAsync(DateOnly fromDate, DateOnly toDate, Guid employeeId, CancellationToken cancellationToken = default);
}
