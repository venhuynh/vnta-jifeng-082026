using System.Globalization;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;

/// <summary>
/// Adapter của Interactive Server: chuyển DTO ứng dụng sang model UI và mở
/// authorization/audit scope trước mọi command có thể thay đổi dữ liệu.
/// </summary>
public sealed class LeaveHolidayAllowanceDataProvider(
    ILeaveHolidayAllowanceReadService leaveHolidayAllowanceReadService,
    ILeaveHolidayAllowancePeriodPreparationService periodPreparationService,
    ILeaveHolidayAllowanceRecalculationService recalculationService,
    ILeaveHolidayAllowanceManualAdjustmentService manualAdjustmentService,
    ILeaveHolidayAllowanceLockService lockService,
    IPayrollAdministrationAuthorizer payrollAdministrationAuthorizer,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory,
    MonthlyWorkSummaryDataProvider monthlyWorkSummaryDataProvider)
    : ILeaveHolidayAllowanceDataProvider
{
    #region Chuẩn bị và đọc dữ liệu

    public async Task PreparePeriodAsync(
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.LeaveHolidayAllowance.PreparePeriod,
            token => periodPreparationService.PreparePeriodAsync(payrollYear, payrollMonth, token),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveHolidayAllowanceRecord>> SearchAsync(
        LeaveHolidayAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = await leaveHolidayAllowanceReadService.SearchAsync(filter, cancellationToken);
        return result.Select(MapRecord).ToArray();
    }

    public async Task<MonthlyWorkSummaryGridRowRecord?> LoadEmployeeMonthlyWorkAsync(
        Guid payrollAllowanceSummaryRecordId,
        Guid employeeId,
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default)
    {
        if (payrollAllowanceSummaryRecordId == Guid.Empty
            || employeeId == Guid.Empty
            || payrollYear is < 1900 or > 2100
            || payrollMonth is < 1 or > 12)
        {
            return null;
        }

        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);

        var records = await leaveHolidayAllowanceReadService.SearchAsync(
            new LeaveHolidayAllowanceFilter(payrollMonth, payrollYear, SearchText: null),
            cancellationToken);
        var belongsToAppliedPeriod = records.Any(record =>
            record.PayrollAllowanceSummaryRecordId == payrollAllowanceSummaryRecordId
            && record.EmployeeId == employeeId
            && record.PayrollYear == payrollYear
            && record.PayrollMonth == payrollMonth);
        if (!belongsToAppliedPeriod)
        {
            return null;
        }

        var fromDate = new DateOnly(payrollYear, payrollMonth, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);
        return await monthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
            fromDate,
            toDate,
            employeeId,
            cancellationToken);
    }

    #endregion

    #region Command nghiệp vụ

    public async Task<RecalculateLeaveHolidayAllowanceResult> RecalculateAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default,
        Guid? payrollAllowanceSummaryRecordId = null)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.LeaveHolidayAllowance.Recalculate,
            token => recalculationService.RecalculateAsync(
                new RecalculateLeaveHolidayAllowanceRequest(
                    payrollMonth,
                    payrollYear,
                    Actor: null,
                    PayrollAllowanceSummaryRecordId: payrollAllowanceSummaryRecordId),
                token),
            cancellationToken: cancellationToken);
    }

    public async Task<LeaveHolidayAllowanceRecord> UpdateManualValuesAsync(
        Guid payrollAllowanceSummaryRecordId,
        decimal dailyWageAmount,
        decimal leaveDayCount,
        decimal holidayDayCount,
        string? note,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.LeaveHolidayAllowance.ManualValuesUpdated,
            token => manualAdjustmentService.UpdateManualValuesAsync(
                new UpdateLeaveHolidayAllowanceManualValuesRequest(
                    payrollAllowanceSummaryRecordId,
                    dailyWageAmount,
                    leaveDayCount,
                    holidayDayCount,
                    note,
                    OriginalUpdatedAtUtc: originalUpdatedAtUtc),
                token),
            cancellationToken: cancellationToken);

        return MapRecord(result);
    }

    public async Task<LeaveHolidayAllowanceRecord> SetLockStateAsync(
        Guid payrollAllowanceSummaryRecordId,
        bool isLocked,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.LeaveHolidayAllowance.LockStateChanged,
            token => lockService.SetLockStateAsync(
                new SetLeaveHolidayAllowanceLockStateRequest(
                    payrollAllowanceSummaryRecordId,
                    isLocked,
                    OriginalUpdatedAtUtc: originalUpdatedAtUtc),
                token),
            cancellationToken: cancellationToken);

        return MapRecord(result);
    }

    public async Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetLeaveHolidayAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var scope = request.PayrollAllowanceSummaryRecordIds is null ? "whole-period" : "selected-rows";
        var requestedRowCount = request.PayrollAllowanceSummaryRecordIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Count() ?? 0;
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.LeaveHolidayAllowance.BatchLockStateChanged,
            token => lockService.SetLockStateBatchAsync(request, token),
            metadata: new Dictionary<string, string>
            {
                ["payrollYear"] = request.PayrollYear.ToString(CultureInfo.InvariantCulture),
                ["payrollMonth"] = request.PayrollMonth.ToString(CultureInfo.InvariantCulture),
                ["scope"] = scope,
                ["targetState"] = request.IsLocked ? "locked" : "unlocked",
                ["requestedRowCount"] = requestedRowCount.ToString(CultureInfo.InvariantCulture)
            },
            cancellationToken: cancellationToken);
    }

    #endregion

    #region Chuyển đổi model

    private static LeaveHolidayAllowanceRecord MapRecord(LeaveHolidayAllowanceListItemDto source)
    {
        return new LeaveHolidayAllowanceRecord
        {
            Id = source.PayrollAllowanceSummaryRecordId,
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            DailyWageAmount = source.DailyWageAmount,
            LeaveDayCount = source.LeaveDayCount,
            HolidayDayCount = source.HolidayDayCount,
            LeaveHolidayAllowanceAmount = source.LeaveHolidayAllowanceAmount,
            Note = source.Note,
            IsLocked = source.IsLocked,
            CreatedAtUtc = source.CreatedAtUtc,
            CreatedBy = source.CreatedBy,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedBy = source.UpdatedBy,
            DetailUpdatedAtUtc = source.DetailUpdatedAtUtc
        };
    }

    #endregion
}
