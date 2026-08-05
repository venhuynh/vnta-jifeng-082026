using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;

internal static class SeniorityAllowanceCommandSupport
{
    internal const string SystemActor = "system";

    public static void ValidatePeriod(int year, int month)
    {
        if(year is < 2026 or > 2100)
            throw new InvalidOperationException("Năm dữ liệu phải nằm trong khoảng 2026 đến 2100.");
        if(month is < 1 or > 12)
            throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
        if(year == 2026 && month < 6)
            throw new InvalidOperationException("Mốc dữ liệu của màn phụ cấp thâm niên bắt đầu từ 06/2026.");
    }

    public static DateTime GetDatabaseNow()
    {
        var now = DateTime.UtcNow.AddHours(7);
        return new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Unspecified);
    }

    public static async Task<PayrollEmployeeSeniorityAllowanceListItemDto> ReadSingleAsync(
        ApplicationDbContext dbContext, Guid summaryRecordId, CancellationToken cancellationToken)
    {
        var period = await dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
            .Where(x => x.Id == summaryRecordId).Select(x => new { x.PayrollYear, x.PayrollMonth })
            .SingleAsync(cancellationToken);
        return await SeniorityAllowanceReadProjection.BuildFilteredQuery(dbContext,
                new PayrollEmployeeSeniorityAllowanceFilter(period.PayrollMonth, period.PayrollYear))
            .Where(x => x.Detail.PayrollAllowanceSummaryRecordId == summaryRecordId)
            .Select(x => SeniorityAllowanceReadProjection.Map(x)).SingleAsync(cancellationToken);
    }
}
