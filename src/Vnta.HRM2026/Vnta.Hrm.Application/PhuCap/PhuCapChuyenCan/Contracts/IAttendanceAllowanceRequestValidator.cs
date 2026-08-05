using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>Validates attendance-allowance requests independently of their transport or persistence adapter.</summary>
public interface IAttendanceAllowanceRequestValidator
{
    AttendanceAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear);
    AttendanceAllowanceValidationResult Validate(RefreshAttendanceAllowanceRequest request);
    AttendanceAllowanceValidationResult Validate(UpdateAttendanceAllowanceActualWorkdayRequest request);
    AttendanceAllowanceValidationResult Validate(UpdateAttendanceAllowanceStandardWorkdayRequest request);
    AttendanceAllowanceValidationResult Validate(SetAttendanceAllowanceLockStateRequest request);
    AttendanceAllowanceValidationResult Validate(SetAttendanceAllowanceBatchLockStateRequest request);
    AttendanceAllowanceValidationResult Validate(AttendanceAllowanceExportRequest request);
}

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
