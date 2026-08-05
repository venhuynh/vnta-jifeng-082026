using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

/// <summary>Builds immutable server filters from the current page snapshot.</summary>
internal interface IPhuCapPhepLeFilterFactory
{
    LeaveHolidayAllowanceFilter CreateListFilter(LeaveHolidayAllowanceReloadRequest snapshot);
}

internal sealed class PhuCapPhepLeFilterFactory : IPhuCapPhepLeFilterFactory
{
    public LeaveHolidayAllowanceFilter CreateListFilter(LeaveHolidayAllowanceReloadRequest snapshot) =>
        new(snapshot.PayrollMonth, snapshot.PayrollYear, snapshot.SearchText);
}
