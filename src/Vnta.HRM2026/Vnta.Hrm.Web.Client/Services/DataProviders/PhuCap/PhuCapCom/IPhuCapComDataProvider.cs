using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapCom;

/// <summary>
/// Contract consumed by the PhuCapCom UI. The component depends on this
/// screen-facing contract instead of the concrete HTTP/UI adapter.
/// </summary>
public interface IPhuCapComDataProvider
{
    Task<MealAllowanceSummaryDto> GetSummaryAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default);

    Task<MealAllowanceLoadResult> SearchPageAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealAllowanceRecord>> ExportPeriodAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default);

    Task<RefreshMealAllowanceResult> RefreshAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default);

    Task<MealAllowanceRecord> UpdateManualValuesAsync(
        Guid id,
        int qualifiedMealDays,
        string? note,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<SetMealAllowanceLockStateBatchResult> SetLockStateBatchAsync(
        SetMealAllowanceLockStateBatchRequest request,
        CancellationToken cancellationToken = default);
}
