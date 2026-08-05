namespace Vnta.Hrm.Application.KhauTru.KhauTruKhac;

public sealed record PreparePayrollEmployeeOtherDeductionAllowancePeriodRequest(int PayrollYear, int PayrollMonth);

public sealed record PayrollEmployeeOtherDeductionAllowanceFilter(
    int PayrollMonth,
    int PayrollYear,
    Guid? EmployeeId = null,
    string? SearchText = null,
    int Take = 5000,
    int Skip = 0);

public sealed record PayrollEmployeeOtherDeductionAllowanceListItemDto(
    Guid Id,
    Guid PayrollDeductionSummaryRecordId,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    string? Description,
    decimal DeductionAmount,
    string? Note,
    bool IsLocked,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PayrollEmployeeOtherDeductionAllowancePageDto(
    IReadOnlyList<PayrollEmployeeOtherDeductionAllowanceListItemDto> Rows,
    int TotalCount);

public sealed record RefreshPayrollEmployeeOtherDeductionAllowanceRequest(
    int PayrollYear,
    int PayrollMonth,
    Guid? PayrollDeductionSummaryRecordId = null);

public sealed record RefreshPayrollEmployeeOtherDeductionAllowanceResult(
    int MatchedRowCount,
    int UpdatedCount,
    int SkippedLockedCount);

public sealed record UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest(
    Guid PayrollDeductionSummaryRecordId,
    decimal DeductionAmount,
    string? Note,
    DateTime? OriginalUpdatedAtUtc);

/// <summary>
/// Báo hiệu dữ liệu Khấu trừ khác đã đổi hoặc bị khóa sau khi người dùng mở form.
/// HTTP boundary ánh xạ ngoại lệ này thành 409 để UI tải lại dữ liệu nguồn.
/// </summary>
public sealed class PayrollEmployeeOtherDeductionConflictException(string message)
    : InvalidOperationException(message);

public sealed record SetPayrollEmployeeOtherDeductionAllowanceLockStateRequest(
    Guid PayrollDeductionSummaryRecordId,
    bool IsLocked);

public sealed record SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyCollection<Guid>? PayrollDeductionSummaryRecordIds);

public sealed record SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateResult(
    int TargetRowCount,
    int UpdatedCount);
