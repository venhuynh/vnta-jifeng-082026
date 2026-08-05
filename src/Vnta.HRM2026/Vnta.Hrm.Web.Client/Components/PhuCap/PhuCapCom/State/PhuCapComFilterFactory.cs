namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>
/// Builds server filters for the current screen snapshot.
/// Keeping this policy outside the component makes query construction
/// independently testable and prevents reload orchestration from owning it.
/// </summary>
internal interface IPhuCapComFilterFactory
{
    MealAllowanceFilter CreateSummaryFilter(
        MealAllowanceReloadSnapshot snapshot,
        int summaryPageSize);

    MealAllowanceFilter CreateListFilter(
        MealAllowanceReloadSnapshot snapshot,
        string? summaryBucketKey);
}

internal sealed class PhuCapComFilterFactory : IPhuCapComFilterFactory
{
    public MealAllowanceFilter CreateSummaryFilter(
        MealAllowanceReloadSnapshot snapshot,
        int summaryPageSize) =>
        new(
            snapshot.PayrollMonth,
            snapshot.PayrollYear,
            snapshot.SearchText,
            summaryPageSize);

    public MealAllowanceFilter CreateListFilter(
        MealAllowanceReloadSnapshot snapshot,
        string? summaryBucketKey) =>
        new(
            snapshot.PayrollMonth,
            snapshot.PayrollYear,
            snapshot.SearchText,
            snapshot.PageSize,
            SummaryBucketKey: summaryBucketKey,
            snapshot.PageIndex * snapshot.PageSize,
            IncludeTotalCount: true);
}
