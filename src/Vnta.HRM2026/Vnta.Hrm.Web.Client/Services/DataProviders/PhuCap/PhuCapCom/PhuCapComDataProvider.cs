using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapCom;

/// <summary>
/// UI adapter owned by the PhuCapCom screen. The MealAllowance application
/// contracts remain the compatibility boundary for the current HTTP API.
/// </summary>
public sealed class PhuCapComDataProvider(
    IMealAllowanceReadService readService,
    IMealAllowanceExportService exportService,
    IMealAllowanceRefreshService refreshService,
    IMealAllowanceManualAdjustmentService manualAdjustmentService,
    IMealAllowanceLockService lockService) : IPhuCapComDataProvider
{
    public Task<MealAllowanceSummaryDto> GetSummaryAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        readService.GetSummaryAsync(filter, cancellationToken);

    public async Task<IReadOnlyList<MealAllowanceRecord>> SearchAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await readService.SearchAsync(filter, cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public async Task<MealAllowanceLoadResult> SearchPageAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await readService.SearchPageAsync(filter, cancellationToken);
        return new MealAllowanceLoadResult(
            page.Rows.Select(MapRecord).ToArray(),
            page.TotalCount);
    }

    public async Task<IReadOnlyList<MealAllowanceRecord>> ExportPeriodAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default)
    {
        var rows = await exportService.ExportPeriodAsync(payrollMonth, payrollYear, cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public Task<RefreshMealAllowanceResult> RefreshAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default) =>
        refreshService.RefreshAsync(
            new RefreshMealAllowanceRequest(targetPayrollMonth, targetPayrollYear, employeeId),
            cancellationToken);

    public async Task<MealAllowanceRecord> UpdateManualValuesAsync(
        Guid id,
        int qualifiedMealDays,
        string? note,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var result = await manualAdjustmentService.UpdateManualValuesAsync(
            new UpdateMealAllowanceManualValuesRequest(
                id,
                qualifiedMealDays,
                note,
                originalUpdatedAtUtc),
            cancellationToken);

        return MapRecord(result);
    }

    public Task<SetMealAllowanceLockStateBatchResult> SetLockStateBatchAsync(
        SetMealAllowanceLockStateBatchRequest request,
        CancellationToken cancellationToken = default) =>
        lockService.SetLockStateBatchAsync(request, cancellationToken);

    private static MealAllowanceRecord MapRecord(MealAllowanceListItemDto source)
    {
        var record = new MealAllowanceRecord
        {
            Id = source.Id,
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            QualifiedMealDays = source.QualifiedMealDays,
            Overtime1900Days = source.Overtime1900Days,
            MealAllowancePerQualifiedDay = source.MealAllowancePerQualifiedDay,
            RuleCode = source.RuleCode,
            RuleVersion = source.RuleVersion,
            Note = source.Note,
            IsLocked = source.IsLocked,
            CalculatedAtUtc = source.CalculatedAtUtc,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

        record.SetServerCalculatedValues(source.MealAllowanceAmount);
        return record;
    }
}

public sealed record MealAllowanceLoadResult(
    IReadOnlyList<MealAllowanceRecord> Rows,
    int TotalCount);
