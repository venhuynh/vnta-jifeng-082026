using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>Validates a payroll period without depending on its transport or persistence adapter.</summary>
public interface IAttendanceAllowancePayrollPeriodValidator
{
    AttendanceAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear);
}

/// <summary>Validates attendance-allowance refresh requests.</summary>
public interface IAttendanceAllowanceRefreshRequestValidator
{
    AttendanceAllowanceValidationResult Validate(RefreshAttendanceAllowanceRequest request);
}

/// <summary>Validates manual attendance-allowance adjustments.</summary>
public interface IAttendanceAllowanceManualAdjustmentRequestValidator
{
    AttendanceAllowanceValidationResult Validate(UpdateAttendanceAllowanceActualWorkdayRequest request);
    AttendanceAllowanceValidationResult Validate(UpdateAttendanceAllowanceStandardWorkdayRequest request);
}

/// <summary>Validates one-row attendance-allowance lock transitions.</summary>
public interface IAttendanceAllowanceLockStateRequestValidator
{
    AttendanceAllowanceValidationResult Validate(SetAttendanceAllowanceLockStateRequest request);
}

/// <summary>Validates attendance-allowance lock transitions for a payroll period or selected rows.</summary>
public interface IAttendanceAllowanceBatchLockRequestValidator
{
    AttendanceAllowanceValidationResult Validate(SetAttendanceAllowanceBatchLockStateRequest request);
}

/// <summary>Validates attendance-allowance export requests.</summary>
public interface IAttendanceAllowanceExportRequestValidator
{
    AttendanceAllowanceValidationResult Validate(AttendanceAllowanceExportRequest request);
}

/// <summary>
/// Backwards-compatible aggregate validator. New consumers should depend only on the narrow capability
/// needed by their workflow.
/// </summary>
public interface IAttendanceAllowanceRequestValidator :
    IAttendanceAllowancePayrollPeriodValidator,
    IAttendanceAllowanceRefreshRequestValidator,
    IAttendanceAllowanceManualAdjustmentRequestValidator,
    IAttendanceAllowanceLockStateRequestValidator,
    IAttendanceAllowanceBatchLockRequestValidator,
    IAttendanceAllowanceExportRequestValidator;

/// <summary>Transport-neutral validation result shared by HTTP endpoints and command workflows.</summary>
public sealed record AttendanceAllowanceValidationResult(string? ErrorMessage)
{
    public bool IsValid => string.IsNullOrWhiteSpace(ErrorMessage);

    public void ThrowIfInvalid()
    {
        if(!IsValid)
            throw new InvalidOperationException(ErrorMessage);
    }
}
