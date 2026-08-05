namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;

/// <summary>Pure authorization and validation rule for a manual holiday-day adjustment.</summary>
public static class LeaveHolidayAllowanceManualAdjustmentPolicy
{
    public static LeaveHolidayAllowanceManualAdjustmentDecision Evaluate(
        LeaveHolidayAllowanceManualAdjustmentInput input)
    {
        if(input.SubmittedDailyWageAmount < 0m)
        {
            return LeaveHolidayAllowanceManualAdjustmentDecision.NegativeDailyWageAmount;
        }

        if(input.SubmittedLeaveDayCount < 0m)
        {
            return LeaveHolidayAllowanceManualAdjustmentDecision.NegativeLeaveDayCount;
        }

        if(input.SubmittedHolidayDayCount < 0m)
        {
            return LeaveHolidayAllowanceManualAdjustmentDecision.NegativeHolidayDayCount;
        }

        if(input.IsAllowanceRecordLocked)
        {
            return LeaveHolidayAllowanceManualAdjustmentDecision.AllowanceRecordLocked;
        }

        var submitted = LeaveHolidayAllowanceCalculationPolicy.Calculate(
            new LeaveHolidayAllowanceCalculationInput(
                input.SubmittedDailyWageAmount,
                input.SubmittedLeaveDayCount,
                input.SubmittedHolidayDayCount));
        var calculated = LeaveHolidayAllowanceCalculationPolicy.Calculate(
            new LeaveHolidayAllowanceCalculationInput(
                input.CalculatedDailyWageAmount,
                input.CalculatedLeaveDayCount,
                0m));

        return submitted.DailyWageAmount != calculated.DailyWageAmount
               || submitted.LeaveDayCount != calculated.LeaveDayCount
            ? LeaveHolidayAllowanceManualAdjustmentDecision.CalculatedSourceValuesChanged
            : LeaveHolidayAllowanceManualAdjustmentDecision.Allowed;
    }
}

/// <summary>Named facts needed to authorize a manual adjustment.</summary>
public sealed record LeaveHolidayAllowanceManualAdjustmentInput(
    bool IsAllowanceRecordLocked,
    decimal CalculatedDailyWageAmount,
    decimal CalculatedLeaveDayCount,
    decimal SubmittedDailyWageAmount,
    decimal SubmittedLeaveDayCount,
    decimal SubmittedHolidayDayCount);

public enum LeaveHolidayAllowanceManualAdjustmentDecision
{
    Allowed,
    AllowanceRecordLocked,
    NegativeDailyWageAmount,
    NegativeLeaveDayCount,
    NegativeHolidayDayCount,
    CalculatedSourceValuesChanged
}
