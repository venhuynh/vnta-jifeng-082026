using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;

public sealed class DatabaseAttendanceAllowanceReadService(
    ApplicationDbContext dbContext,
    IAttendanceAllowanceWorkdaySource workdaySource) : IAttendanceAllowanceReadService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 5000;

    public async Task<AttendanceAllowanceRuleDto> GetRuleAsync(CancellationToken cancellationToken = default) =>
        new(await workdaySource.LoadEligibleStatusCodesAsync(cancellationToken));

    public async Task<AttendanceAllowanceResultPageDto> SearchPageAsync(AttendanceAllowanceResultFilter filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = AttendanceAllowanceReadProjection.BuildQuery(dbContext, filter, true);
        var overview = AttendanceAllowanceReadProjection.BuildQuery(dbContext, filter with { LockState = AttendanceAllowanceLockState.All, AttendanceClass = null }, true);
        var period = AttendanceAllowanceReadProjection.BuildQuery(dbContext, filter with { SearchText = null, LockState = AttendanceAllowanceLockState.All, AttendanceClass = null }, true);
        var total = await query.CountAsync(cancellationToken);
        var open = await overview.CountAsync(x => !x.Detail.IsLocked && !x.Summary.IsLocked, cancellationToken);
        var locked = await overview.CountAsync(x => x.Detail.IsLocked || x.Summary.IsLocked, cancellationToken);
        var a = await overview.CountAsync(x => x.Detail.AttendanceClass == AttendanceAllowanceClass.A.ToStorageValue(), cancellationToken);
        var b = await overview.CountAsync(x => x.Detail.AttendanceClass == AttendanceAllowanceClass.B.ToStorageValue(), cancellationToken);
        var c = await overview.CountAsync(x => x.Detail.AttendanceClass == AttendanceAllowanceClass.C.ToStorageValue(), cancellationToken);
        var periodTotal = await period.CountAsync(cancellationToken);
        var canLock = await period.CountAsync(x => !x.Detail.IsLocked && !x.Summary.IsLocked, cancellationToken);
        var canUnlock = await period.CountAsync(x => x.Detail.IsLocked && !x.Summary.IsLocked, cancellationToken);
        var summaryLocked = await period.CountAsync(x => x.Summary.IsLocked, cancellationToken);
        var skip = Math.Max(0, filter.Skip);
        if(total == 0 || skip >= total) return new([], total, open, locked, a, b, c, periodTotal, canLock, canUnlock, summaryLocked);
        var rows = await AttendanceAllowanceReadProjection.ApplyStableOrder(query).Skip(skip).Take(filter.Take <= 0 ? DefaultPageSize : Math.Min(filter.Take, MaximumPageSize)).Select(x => AttendanceAllowanceReadProjection.MapToDto(x)).ToListAsync(cancellationToken);
        return new(rows, total, open, locked, a, b, c, periodTotal, canLock, canUnlock, summaryLocked);
    }
}
