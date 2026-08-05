using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

/// <summary>Validates leave/holiday allowance inputs at application boundaries.</summary>
public interface ILeaveHolidayAllowanceRequestValidator
{
    LeaveHolidayAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear);
    LeaveHolidayAllowanceValidationResult Validate(LeaveHolidayAllowanceFilter filter);
    LeaveHolidayAllowanceValidationResult Validate(ClearLeaveHolidayAllowanceManualValuesRequest request);
    LeaveHolidayAllowanceValidationResult Validate(SyncLeaveHolidayAllowanceFromPreviousMonthRequest request);
    LeaveHolidayAllowanceValidationResult Validate(RecalculateLeaveHolidayAllowanceRequest request);
    LeaveHolidayAllowanceValidationResult Validate(UpdateLeaveHolidayAllowanceManualValuesRequest request);
    LeaveHolidayAllowanceValidationResult Validate(SetLeaveHolidayAllowanceLockStateRequest request);
    LeaveHolidayAllowanceValidationResult Validate(SetLeaveHolidayAllowanceBatchLockStateRequest request);
}

/// <summary>Transport-neutral result shared by HTTP and use-case adapters.</summary>
public sealed record LeaveHolidayAllowanceValidationResult(string? ErrorMessage)
{
    public bool IsValid => string.IsNullOrWhiteSpace(ErrorMessage);

    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new InvalidOperationException(ErrorMessage);
    }
}
