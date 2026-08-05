namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;

public sealed record MealAllowanceFilter(
    int? PayrollMonth,
    int? PayrollYear,
    string? SearchText,
    int Take = 2000,
    string? SummaryBucketKey = null,
    int Skip = 0,
    bool IncludeTotalCount = true);
