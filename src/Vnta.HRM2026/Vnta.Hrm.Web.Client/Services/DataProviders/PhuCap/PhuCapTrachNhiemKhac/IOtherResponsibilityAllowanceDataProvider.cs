using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemKhac;

public interface IOtherResponsibilityAllowanceDataProvider
{
    Task<IReadOnlyList<OtherResponsibilityAllowanceRecord>> SearchAsync(
        OtherResponsibilityAllowanceFilter filter,
        CancellationToken cancellationToken = default);

    Task<RecalculateOtherResponsibilityAllowanceResult> RecalculateAsync(
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default);

    Task<SetOtherResponsibilityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        int payrollYear,
        int payrollMonth,
        bool isLocked,
        IReadOnlyCollection<OtherResponsibilityAllowanceRecord>? records,
        CancellationToken cancellationToken = default);
}
