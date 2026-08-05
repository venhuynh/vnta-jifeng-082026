using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

/// <summary>
/// Small contract for screens that only need to read monthly work summaries.
/// </summary>
public interface IMonthlyWorkSummaryDataProvider
{
    Task<MonthlyWorkSummaryGridRowRecord?> LoadEmployeeMonthAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
