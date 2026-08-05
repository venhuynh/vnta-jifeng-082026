using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.TongQuan.ChamCongHangNgay;

public sealed class DatabaseAttendanceDailySummaryService(ApplicationDbContext dbContext)
    : IAttendanceDailySummaryService
{
    public async Task<RebuildAttendanceDailySummaryResult> RebuildAsync(
        RebuildAttendanceDailySummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var effectiveFromDate = request.FromDate;
        var effectiveToDate = request.ToDate;

        if(effectiveToDate < effectiveFromDate)
        {
            (effectiveFromDate, effectiveToDate) = (effectiveToDate, effectiveFromDate);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM attendance_daily_summaries
            WHERE "WorkDate" >= {effectiveFromDate}
              AND "WorkDate" <= {effectiveToDate};
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO attendance_daily_summaries
            (
                "Id",
                "EmployeeId",
                "WorkDate",
                "PunchCount",
                "PunchMomentsText",
                "FirstPunchTime",
                "LastPunchTime",
                "CreatedAtUtc",
                "UpdatedAtUtc"
            )
            SELECT
                gen_random_uuid(),
                a."EmployeeId",
                a."AttTime"::date AS "WorkDate",
                COUNT(*)::int AS "PunchCount",
                STRING_AGG(TO_CHAR(a."AttTime", 'HH24:MI:SS'), '|' ORDER BY a."AttTime") AS "PunchMomentsText",
                MIN(a."AttTime") AS "FirstPunchTime",
                MAX(a."AttTime") AS "LastPunchTime",
                NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh' AS "CreatedAtUtc",
                NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh' AS "UpdatedAtUtc"
            FROM attendance_logs AS a
            WHERE a."AttTime" IS NOT NULL
              AND a."EmployeeId" IS NOT NULL
              AND a."AttTime"::date >= {effectiveFromDate}
              AND a."AttTime"::date <= {effectiveToDate}
            GROUP BY a."EmployeeId", a."AttTime"::date;
            """,
            cancellationToken);

        var rebuiltSummaryCount = await dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(x => x.WorkDate >= effectiveFromDate && x.WorkDate <= effectiveToDate)
            .CountAsync(cancellationToken);

        var totalPunchCount = await dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(x => x.WorkDate >= effectiveFromDate && x.WorkDate <= effectiveToDate)
            .SumAsync(x => (int?)x.PunchCount, cancellationToken) ?? 0;

        await transaction.CommitAsync(cancellationToken);

        return new RebuildAttendanceDailySummaryResult(
            effectiveFromDate,
            effectiveToDate,
            rebuiltSummaryCount,
            totalPunchCount);
    }
}
