using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

/// <summary>
/// Cung cấp tập nhân viên chuẩn của một kỳ lương: mỗi dòng Summary là một nhân viên
/// thuộc phạm vi xử lý phụ cấp của kỳ đó. Các read model chỉ được phép bổ sung dữ liệu
/// lên tập này, không được dùng bảng assignment làm nguồn danh sách.
/// </summary>
internal static class PayrollAllowanceSummaryPopulationQuery
{
    public static IQueryable<PayrollAllowanceSummaryRecordRow> All(ApplicationDbContext dbContext) =>
        dbContext.PayrollAllowanceSummaryRecords.AsNoTracking();

    public static IQueryable<PayrollAllowanceSummaryRecordRow> ForPeriod(
        ApplicationDbContext dbContext,
        int payrollYear,
        int payrollMonth) =>
        All(dbContext).Where(summary =>
            summary.PayrollYear == payrollYear && summary.PayrollMonth == payrollMonth);
}
