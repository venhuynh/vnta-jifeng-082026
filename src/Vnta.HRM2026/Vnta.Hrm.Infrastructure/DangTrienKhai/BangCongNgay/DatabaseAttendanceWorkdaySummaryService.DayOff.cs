using Microsoft.EntityFrameworkCore.Storage;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed partial class DatabaseAttendanceWorkdaySummaryService
{
    private const int DayOffMinimumAttendanceMinutes = 240;
    private const int DayOffFullShiftOvertimeMinutes20 = 480;

    private async Task<RebuildAttendanceWorkdaySummaryResult> RebuildDayOffAsync(
        DateOnly workDate,
        CancellationToken cancellationToken)
    {
        var context = await BuildRebuildContextAsync(
            workDate,
            AttendanceWorkCalendarDayType.DayOff,
            includeEligibleEmployees: false,
            cancellationToken);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var expectedKeys = new HashSet<(Guid EmployeeId, DateOnly WorkDate)>();

        foreach(var punchGroup in context.PunchGroups)
        {
            var key = (punchGroup.EmployeeId, workDate);
            expectedKeys.Add(key);

            if(IsLockedSummaryRow(context, key))
            {
                continue;
            }

            var checkInAt = FormatPunchTime(punchGroup.FirstPunchAt);
            var checkOutAt = punchGroup.PunchCount > 1
                ? FormatPunchTime(punchGroup.LastPunchAt)
                : null;
            var evaluation = EvaluateDayOffAttendance(context, punchGroup);

            UpsertSummaryRow(
                context,
                punchGroup.EmployeeId,
                checkInAt,
                checkOutAt,
                evaluation.StatusCodeId,
                evaluation.Note);

            if(context.RowsByKey.TryGetValue(key, out var row)
                && !row.IsLocked)
            {
                ApplyDayOffAttendance(row, evaluation);
            }
        }

        DeleteUnlockedRowsOutsideKeys(context, expectedKeys);

        return await SaveAndBuildResultAsync(context, transaction, cancellationToken);
    }

    private static bool IsLockedSummaryRow(
        WorkdaySummaryRebuildContext context,
        (Guid EmployeeId, DateOnly WorkDate) key) =>
        context.RowsByKey.TryGetValue(key, out var row) && row.IsLocked;

    private static void ApplyDayOffAttendance(
        AttendanceWorkdaySummaryRow row,
        DayOffAttendanceEvaluation evaluation)
    {
        row.LateMinutes = 0;
        row.EarlyLeaveMinutes = 0;
        row.OvertimeMinutes = evaluation.OvertimeMinutes;
        row.OvertimeMinutes15 = 0;
        row.OvertimeMinutes20 = evaluation.OvertimeMinutes20;
        row.OvertimeMinutes30 = 0;
        row.CheckInForOT15 = null;
        row.IsRegisterForOT = evaluation.IsRegisterForOT;
        row.RequireDocument = evaluation.RequireDocument;
    }
}
