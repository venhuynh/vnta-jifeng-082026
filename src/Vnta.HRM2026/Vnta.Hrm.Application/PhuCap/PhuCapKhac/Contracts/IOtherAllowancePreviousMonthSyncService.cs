using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;

/// <summary>Copies missing other-allowance lines from the preceding payroll period.</summary>
public interface IOtherAllowancePreviousMonthSyncService
{
    Task<SyncOtherAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncOtherAllowanceFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default);
}
