namespace Vnta.Hrm.Web.Client.Models;

// Kết quả dành cho UI phải kèm tổng số server-side để pager không suy ra total từ page hiện tại.
public sealed record MonthlyWorkSummaryLoadResult(
    IReadOnlyList<MonthlyWorkSummaryGridRowRecord> Rows,
    int TotalCount);
