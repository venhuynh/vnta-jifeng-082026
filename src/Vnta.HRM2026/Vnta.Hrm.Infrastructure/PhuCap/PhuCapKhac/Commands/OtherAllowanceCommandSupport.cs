using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

internal static class OtherAllowanceSummarySynchronizer
{
    public static async Task SyncAsync(ApplicationDbContext dbContext, PayrollAllowanceSummaryRecordRow summary, CancellationToken cancellationToken)
    {
        var lines = await dbContext.PayrollOtherAllowanceRecords
            .Where(row => row.PayrollAllowanceSummaryRecordId == summary.Id)
            .Select(row => new OtherAllowanceSummaryLine(row.AllowanceAmount))
            .ToListAsync(cancellationToken);
        summary.OtherAllowanceAmount = OtherAllowanceSummaryAmountCalculator.CalculateTotal(lines);
    }
}
