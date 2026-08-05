using Microsoft.EntityFrameworkCore.Storage;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed partial class DatabaseAttendanceWorkdaySummaryService
{
    private async Task<RebuildAttendanceWorkdaySummaryResult> RebuildPunchOnlyDayAsync(
        DateOnly workDate,
        AttendanceWorkCalendarDayType dayType,
        CancellationToken cancellationToken)
    {
        var context = await BuildRebuildContextAsync(
            workDate,
            dayType,
            includeEligibleEmployees: false,
            cancellationToken);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var expectedKeys = new HashSet<(Guid EmployeeId, DateOnly WorkDate)>();

        foreach(var punchGroup in context.PunchGroups)
        {
            var key = (punchGroup.EmployeeId, workDate);
            expectedKeys.Add(key);

            var statusCodeId = ResolveStatusCodeId(context.StatusCodeIds, punchGroup.PunchCount);
            var note = punchGroup.PunchCount > 1 ? null : PartialPunchNote;
            var checkInAt = FormatPunchTime(punchGroup.FirstPunchAt);
            var checkOutAt = punchGroup.PunchCount > 1
                ? FormatPunchTime(punchGroup.LastPunchAt)
                : null;

            UpsertSummaryRow(
                context,
                punchGroup.EmployeeId,
                checkInAt,
                checkOutAt,
                statusCodeId,
                note);
        }

        DeleteUnlockedRowsOutsideKeys(context, expectedKeys);

        return await SaveAndBuildResultAsync(context, transaction, cancellationToken);
    }
}
