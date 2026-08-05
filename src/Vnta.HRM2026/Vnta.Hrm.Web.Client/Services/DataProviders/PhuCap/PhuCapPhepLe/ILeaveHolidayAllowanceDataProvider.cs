using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;

/// <summary>Screen-facing operations for the leave/holiday allowance page.</summary>
public interface ILeaveHolidayAllowanceDataProvider
{
    Task PreparePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveHolidayAllowanceRecord>> SearchAsync(LeaveHolidayAllowanceFilter filter, CancellationToken cancellationToken = default);
    Task<MonthlyWorkSummaryGridRowRecord?> LoadEmployeeMonthlyWorkAsync(Guid payrollAllowanceSummaryRecordId, Guid employeeId, int payrollYear, int payrollMonth, CancellationToken cancellationToken = default);
    Task<RecalculateLeaveHolidayAllowanceResult> RecalculateAsync(int payrollMonth, int payrollYear, CancellationToken cancellationToken = default, Guid? payrollAllowanceSummaryRecordId = null);
    Task<LeaveHolidayAllowanceRecord> UpdateManualValuesAsync(Guid payrollAllowanceSummaryRecordId, decimal dailyWageAmount, decimal leaveDayCount, decimal holidayDayCount, string? note, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default);
    Task<LeaveHolidayAllowanceRecord> SetLockStateAsync(Guid payrollAllowanceSummaryRecordId, bool isLocked, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default);
    Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetLeaveHolidayAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default);
}
