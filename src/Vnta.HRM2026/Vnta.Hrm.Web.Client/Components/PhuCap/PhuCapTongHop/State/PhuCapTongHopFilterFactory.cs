namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Builds server filters from an immutable page reload snapshot.</summary>
internal interface IPhuCapTongHopFilterFactory
{
    PayrollAllowanceSummaryFilter CreateSummaryFilter(PhuCapTongHop.PayrollAllowanceSummaryReloadSnapshot snapshot);
    PayrollAllowanceSummaryFilter CreateListFilter(PhuCapTongHop.PayrollAllowanceSummaryReloadSnapshot snapshot);
}

internal sealed class PhuCapTongHopFilterFactory : IPhuCapTongHopFilterFactory
{
    public PayrollAllowanceSummaryFilter CreateSummaryFilter(PhuCapTongHop.PayrollAllowanceSummaryReloadSnapshot snapshot) =>
        new(snapshot.PayrollMonth, snapshot.PayrollYear, snapshot.SearchText);

    public PayrollAllowanceSummaryFilter CreateListFilter(PhuCapTongHop.PayrollAllowanceSummaryReloadSnapshot snapshot) =>
        new(snapshot.PayrollMonth, snapshot.PayrollYear, snapshot.SearchText, snapshot.IsLocked, snapshot.PageIndex * snapshot.PageSize, snapshot.PageSize);
}
