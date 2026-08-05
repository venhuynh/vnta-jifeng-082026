using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

/// <summary>
/// Boundary giữa UI khấu trừ phí công đoàn và contract nghiệp vụ của payroll.
/// </summary>
public sealed class PayrollUnionFeeDeductionDataProvider(
    IPayrollUnionFeeDeductionReadService readService,
    IPayrollUnionFeeDeductionPeriodPreparationService periodPreparationService,
    IPayrollUnionFeeDeductionRefreshService refreshService,
    IPayrollUnionFeeDeductionManualAdjustmentService manualAdjustmentService,
    IPayrollUnionFeeDeductionLockService lockService)
{
    public Task<PreparePayrollUnionFeeDeductionPeriodResult> PreparePeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        periodPreparationService.PreparePeriodAsync(year, month, cancellationToken);

    public Task<RefreshPayrollUnionFeeDeductionResult> RefreshAsync(
        RefreshPayrollUnionFeeDeductionRequest request,
        CancellationToken cancellationToken = default) =>
        refreshService.RefreshAsync(request, cancellationToken);

    public async Task<PayrollUnionFeeDeductionLoadResult> SearchAsync(
        PayrollUnionFeeDeductionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await readService.SearchAsync(filter, cancellationToken);
        return new PayrollUnionFeeDeductionLoadResult(
            page.Items.Select(MapRecord).ToArray(),
            page.TotalCount);
    }

    public async Task<PayrollUnionFeeDeductionRecord> SetLockStateAsync(
        PayrollUnionFeeDeductionRecord record,
        bool isLocked,
        CancellationToken cancellationToken = default)
    {
        var result = await lockService.SetLockStateAsync(
            new SetPayrollUnionFeeDeductionLockStateRequest(
                record.Id,
                isLocked,
                record.UpdatedAtUtc),
            cancellationToken);

        return MapRecord(result);
    }

    public async Task<PayrollUnionFeeDeductionRecord> UpdateManualValueAsync(
        Guid payrollDeductionSummaryRecordId,
        decimal deductionAmount,
        DateTime originalVersionAtUtc,
        CancellationToken cancellationToken = default) =>
        MapRecord(await manualAdjustmentService.UpdateManualValueAsync(
            new UpdatePayrollUnionFeeDeductionManualValueRequest(
                payrollDeductionSummaryRecordId,
                deductionAmount,
                originalVersionAtUtc),
            cancellationToken));

    public Task<SetPayrollUnionFeeDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollUnionFeeDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        lockService.SetLockStateBatchAsync(request, cancellationToken);

    private static PayrollUnionFeeDeductionRecord MapRecord(PayrollUnionFeeDeductionListItemDto source) => new()
    {
        Id = source.PayrollDeductionSummaryRecordId,
        EmployeeId = source.EmployeeId,
        EmployeeCode = source.EmployeeCode,
        EmployeeName = source.EmployeeName,
        DepartmentName = source.DepartmentName,
        PositionName = source.PositionName,
        PayrollMonth = source.PayrollMonth,
        PayrollYear = source.PayrollYear,
        DeductionAmount = source.DeductionAmount,
        IsSummaryLocked = source.IsSummaryLocked,
        IsLocked = source.IsLocked,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc
    };
}

public sealed record PayrollUnionFeeDeductionLoadResult(
    IReadOnlyList<PayrollUnionFeeDeductionRecord> Rows,
    int TotalCount);
