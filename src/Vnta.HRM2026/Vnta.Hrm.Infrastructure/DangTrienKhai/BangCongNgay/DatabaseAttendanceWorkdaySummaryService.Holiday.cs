using Microsoft.EntityFrameworkCore.Storage;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed partial class DatabaseAttendanceWorkdaySummaryService
{
    private const int HolidayMinimumAttendanceMinutes = 240;
    private const int HolidayFullShiftOvertimeMinutes30 = 480;

    private async Task<RebuildAttendanceWorkdaySummaryResult> RebuildHolidayAsync(
        DateOnly workDate,
        CancellationToken cancellationToken)
    {
        var context = await BuildRebuildContextAsync(
            workDate,
            AttendanceWorkCalendarDayType.Holiday,
            includeEligibleEmployees: false,
            cancellationToken);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var expectedKeys = new HashSet<(Guid EmployeeId, DateOnly WorkDate)>();

        foreach(var punchGroup in context.PunchGroups)
        {
            var key = (punchGroup.EmployeeId, workDate);
            expectedKeys.Add(key);

            if(context.RowsByKey.TryGetValue(key, out var lockedRow) && lockedRow.IsLocked)
            {
                continue;
            }

            var checkInAt = FormatPunchTime(punchGroup.FirstPunchAt);
            var checkOutAt = punchGroup.PunchCount > 1
                ? FormatPunchTime(punchGroup.LastPunchAt)
                : null;
            var evaluation = EvaluateHolidayAttendance(context, punchGroup);

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
                ApplyHolidayAttendance(row, evaluation);
            }
        }

        DeleteUnlockedRowsOutsideKeys(context, expectedKeys);

        return await SaveAndBuildResultAsync(context, transaction, cancellationToken);
    }

    private static void ApplyHolidayAttendance(
        AttendanceWorkdaySummaryRow row,
        HolidayAttendanceEvaluation evaluation)
    {
        row.LateMinutes = 0;
        row.EarlyLeaveMinutes = 0;
        row.OvertimeMinutes = evaluation.OvertimeMinutes;
        row.OvertimeMinutes15 = 0;
        row.OvertimeMinutes20 = 0;
        row.OvertimeMinutes30 = evaluation.OvertimeMinutes30;
        row.CheckInForOT15 = null;
        row.IsRegisterForOT = evaluation.IsRegisterForOT;
        row.RequireDocument = evaluation.RequireDocument;
    }
}
