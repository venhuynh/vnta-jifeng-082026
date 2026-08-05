namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed partial class DatabaseAttendanceWorkdaySummaryService
{
    private HolidayAttendanceEvaluation EvaluateHolidayAttendance(
        WorkdaySummaryRebuildContext context,
        WorkdayPunchAggregate punchGroup)
    {
        var hasRegisteredOvertime = IsHolidayOvertimeRegistrationSatisfied();

        if(punchGroup.PunchCount <= 1)
        {
            return CreateHolidayAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        if(!TryBuildActualPunchRange(punchGroup, out var actualRange))
        {
            return CreateHolidayAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        var actualDurationMinutes = CalculatePositiveMinutes(actualRange.CheckIn, actualRange.CheckOut);
        if(actualDurationMinutes < HolidayMinimumAttendanceMinutes)
        {
            return CreateHolidayAbnormalEvaluation(context, hasRegisteredOvertime);
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
            return CreateHolidayAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        if(actualRange.CheckIn > scheduledRange.Start || actualRange.CheckOut < scheduledRange.End)
        {
            return CreateHolidayAbnormalEvaluation(context, hasRegisteredOvertime);
        }

        return new HolidayAttendanceEvaluation(
            ResolveRequiredStatusCodeId(context.StatusCodeIds, FullWorkStatusCode),
            null,
            HolidayFullShiftOvertimeMinutes30,
            HolidayFullShiftOvertimeMinutes30,
            hasRegisteredOvertime,
            false);
    }

    private bool IsHolidayOvertimeRegistrationSatisfied()
    {
        EnsureDailyOvertimeRegistrationCheckIsSatisfied();
        return true;
    }

    private HolidayAttendanceEvaluation CreateHolidayAbnormalEvaluation(
        WorkdaySummaryRebuildContext context,
        bool hasRegisteredOvertime)
    {
        return new HolidayAttendanceEvaluation(
            ResolveRequiredStatusCodeId(context.StatusCodeIds, AbnormalStatusCode),
            null,
            0,
            0,
            hasRegisteredOvertime,
            false);
    }

    private sealed record HolidayAttendanceEvaluation(
        Guid StatusCodeId,
        string? Note,
        int OvertimeMinutes,
        int OvertimeMinutes30,
        bool IsRegisterForOT,
        bool RequireDocument);
}
