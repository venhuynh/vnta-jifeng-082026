using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapChuyenCan;

/// <summary>
/// Boundary của màn hình Phụ cấp chuyên cần với các tác vụ đọc và điều phối command.
/// Component chỉ phụ thuộc contract này, còn transport/audit thuộc implementation.
/// </summary>
public interface IAttendanceAllowanceReadDataProvider
{
    Task<AttendanceAllowanceRuleDto> GetRuleAsync(
        CancellationToken cancellationToken = default);

    Task<AttendanceAllowanceResultLoadResult> SearchPageAsync(
        AttendanceAllowanceResultFilter filter,
        CancellationToken cancellationToken = default);
}

public interface IAttendanceAllowanceExportDataProvider
{

    Task<IReadOnlyList<AttendanceAllowanceExportRowDto>> ExportAsync(
        int payrollYear,
        int payrollMonth,
        AttendanceAllowanceExportFormat format,
        CancellationToken cancellationToken = default);
}

public interface IAttendanceAllowanceRefreshDataProvider
{

    Task<RefreshAttendanceAllowanceResult> RefreshAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        CancellationToken cancellationToken = default);

    Task<RefreshAttendanceAllowanceResult> RefreshRowAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        Guid payrollAllowanceSummaryRecordId,
        CancellationToken cancellationToken = default);
}

public interface IAttendanceAllowanceManualAdjustmentDataProvider
{

    Task<AttendanceAllowanceResultRecord> UpdateActualWorkdayAsync(
        Guid id,
        decimal actualWorkdayCount,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<AttendanceAllowanceResultRecord> UpdateStandardWorkdayAsync(
        Guid id,
        decimal standardWorkdayCount,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default);
}

public interface IAttendanceAllowanceLockDataProvider
{

    Task<SetAttendanceAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        int payrollYear,
        int payrollMonth,
        bool isLocked,
        IEnumerable<AttendanceAllowanceResultRecord>? records = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Backward-compatible composite contract for existing consumers and tests.
/// New screens should depend on the smallest capability interface they need.
/// </summary>
public interface IAttendanceAllowanceResultDataProvider :
    IAttendanceAllowanceReadDataProvider,
    IAttendanceAllowanceExportDataProvider,
    IAttendanceAllowanceRefreshDataProvider,
    IAttendanceAllowanceManualAdjustmentDataProvider,
    IAttendanceAllowanceLockDataProvider
{
}
