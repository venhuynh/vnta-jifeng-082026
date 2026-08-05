namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed partial class DatabaseAttendanceWorkdaySummaryService
{
    private DayOffAttendanceEvaluation EvaluateDayOffAttendance(
        WorkdaySummaryRebuildContext context,
        WorkdayPunchAggregate punchGroup)
    {
        var hasRegisteredOvertime = IsDayOffOvertimeRegistrationSatisfied();

        if(punchGroup.PunchCount <= 1)
        {
            return CreateDayOffAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        if(!TryBuildActualPunchRange(punchGroup, out var actualRange))
        {
            return CreateDayOffAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        var actualDurationMinutes = CalculatePositiveMinutes(actualRange.CheckIn, actualRange.CheckOut);
        if(actualDurationMinutes < DayOffMinimumAttendanceMinutes)
        {
            return CreateDayOffAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        var shiftInfo = ResolveShiftInfo(
            context.EmployeeProfiles,
            context.ShiftsById,
            context.ShiftAssignmentsByKey,
            context.ShiftSchedulingSettings,
            punchGroup.EmployeeId,
            context.WorkDate);

        if(shiftInfo is null
            || !TryBuildWorkdayTimeRange(context.WorkDate, shiftInfo.ScheduledStartAt, shiftInfo.ScheduledEndAt, out var scheduledRange))
        {
            return CreateDayOffAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        if(actualRange.CheckIn > scheduledRange.Start || actualRange.CheckOut < scheduledRange.End)
        {
            return CreateDayOffAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        return new DayOffAttendanceEvaluation(
            ResolveRequiredStatusCodeId(context.StatusCodeIds, FullWorkStatusCode),
            null,
            DayOffFullShiftOvertimeMinutes20,
            DayOffFullShiftOvertimeMinutes20,
            hasRegisteredOvertime,
            false);
    }

    private bool IsDayOffOvertimeRegistrationSatisfied()
    {
        EnsureDailyOvertimeRegistrationCheckIsSatisfied();
        return true;
    }

    private DayOffAttendanceEvaluation CreateDayOffAbnormalEvaluation(
        WorkdaySummaryRebuildContext context,
        bool hasRegisteredOvertime)
    {
        return new DayOffAttendanceEvaluation(
            ResolveRequiredStatusCodeId(context.StatusCodeIds, AbnormalStatusCode),
            null,
            0,
            0,
            hasRegisteredOvertime,
            false);
    }

    private sealed record DayOffAttendanceEvaluation(
        Guid StatusCodeId,
        string? Note,
        int OvertimeMinutes,
        int OvertimeMinutes20,
        bool IsRegisterForOT,
        bool RequireDocument);
}
