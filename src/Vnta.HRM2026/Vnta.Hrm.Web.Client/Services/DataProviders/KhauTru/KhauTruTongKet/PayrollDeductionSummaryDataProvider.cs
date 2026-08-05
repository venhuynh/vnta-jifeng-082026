using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummaryDataProvider(
    IPayrollDeductionSummaryReadService readService,
    IPayrollDeductionSummaryExportService exportService,
    IPayrollDeductionSummarySyncService syncService,
    IPayrollDeductionSummaryRefreshService refreshService,
    IPayrollDeductionSummaryManualAdjustmentService manualAdjustmentService,
    IPayrollDeductionSummaryLockService lockService)
{
    public async Task<PayrollDeductionSummaryLoadResult> SearchAsync(
        PayrollDeductionSummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await readService.SearchAsync(filter, cancellationToken);
        return new PayrollDeductionSummaryLoadResult(
            page.Rows.Select(MapRecord).ToArray(),
            page.TotalCount,
            MapTotals(page.Totals),
            MapLockStatusCounts(page.LockStatusCounts));
    }

    public async Task<IReadOnlyList<PayrollDeductionSummaryExportRecord>> LoadAllForPeriodExportAsync(
        int payrollYear,
        int payrollMonth,
        PayrollDeductionSummaryExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var rows = await exportService.ExportPeriodAsync(
            payrollMonth,
            payrollYear,
            format,
            cancellationToken);
        return rows.Select(row => MapExportRecord(row, format)).ToArray();
    }

    public Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        CancellationToken cancellationToken = default) =>
        syncService.SyncFromPreviousMonthAsync(
            new SyncPayrollDeductionSummaryFromPreviousMonthRequest(targetPayrollMonth, targetPayrollYear, Actor: null),
            cancellationToken);

    public Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(
        RefreshPayrollDeductionSummaryRequest request,
        CancellationToken cancellationToken = default) =>
        refreshService.RefreshAsync(request, cancellationToken);

    public Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default) =>
        refreshService.RecalculatePeriodAsync(
            new RecalculatePayrollDeductionSummaryPeriodRequest(payrollYear, payrollMonth),
            cancellationToken);

    public async Task<PayrollDeductionSummaryRecord> SetLockStateAsync(
        Guid id,
        bool isLocked,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var row = await lockService.SetLockStateAsync(
            new SetPayrollDeductionSummaryLockStateRequest(id, isLocked, originalUpdatedAtUtc),
            cancellationToken);

        return MapRecord(row);
    }

    public Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollDeductionSummaryBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        lockService.SetLockStateBatchAsync(request, cancellationToken);

    public async Task<PayrollDeductionSummaryRecord> UpdateManualOtherDeductionAsync(
        Guid id,
        decimal otherDeductionAmount,
        string? note,
        DateTime originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var row = await manualAdjustmentService.UpdateManualOtherDeductionAsync(
            new UpdatePayrollDeductionSummaryManualOtherDeductionRequest(
                id,
                otherDeductionAmount,
                note,
                originalUpdatedAtUtc),
            cancellationToken);

        return MapRecord(row);
    }

    private static PayrollDeductionSummaryRecord MapRecord(PayrollDeductionSummaryListItemDto source) =>
        new()
        {
            Id = source.Id,
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            SocialInsuranceDeductionAmount = source.SocialInsuranceDeductionAmount,
            PersonalIncomeTaxDeductionAmount = source.PersonalIncomeTaxDeductionAmount,
            UnionFeeDeductionAmount = source.UnionFeeDeductionAmount,
            AdvanceDeductionAmount = source.AdvanceDeductionAmount,
            OtherDeductionAmount = source.OtherDeductionAmount,
            IsLocked = source.IsLocked,
            Note = source.Note,
            CreatedAtUtc = source.CreatedAtUtc,
            CreatedBy = source.CreatedBy,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedBy = source.UpdatedBy
        };

    private static PayrollDeductionSummaryExportRecord MapExportRecord(
        PayrollDeductionSummaryExportItemDto source,
        PayrollDeductionSummaryExportFormat format) =>
        new()
        {
            EmployeeDisplay = SanitizeExportText(source.EmployeeDisplay, format),
            DepartmentDisplay = SanitizeExportText(source.DepartmentDisplay, format),
            PositionDisplay = SanitizeExportText(source.PositionDisplay, format),
            PayrollPeriodDisplay = SanitizeExportText(source.PayrollPeriodDisplay, format),
            SocialInsuranceDeductionAmount = source.SocialInsuranceDeductionAmount,
            PersonalIncomeTaxDeductionAmount = source.PersonalIncomeTaxDeductionAmount,
            UnionFeeDeductionAmount = source.UnionFeeDeductionAmount,
            AdvanceDeductionAmount = source.AdvanceDeductionAmount,
            OtherDeductionAmount = source.OtherDeductionAmount,
            TotalDeductionAmount = source.TotalDeductionAmount,
            LockStatusText = SanitizeExportText(source.LockStatusText, format)
        };

    private static string SanitizeExportText(
        string value,
        PayrollDeductionSummaryExportFormat format)
    {
        if(format != PayrollDeductionSummaryExportFormat.Excel
           || string.IsNullOrEmpty(value)
           || value[0] is not ('=' or '+' or '-' or '@'))
        {
            return value;
        }

        return $"'{value}";
    }

    private static PayrollDeductionSummaryTotals MapTotals(PayrollDeductionSummaryAggregateDto source) =>
        new(
            source.SocialInsuranceDeductionAmount,
            source.PersonalIncomeTaxDeductionAmount,
            source.UnionFeeDeductionAmount,
            source.AdvanceDeductionAmount,
            source.OtherDeductionAmount,
            source.TotalDeductionAmount);

    private static PayrollDeductionSummaryLockStatusCounts MapLockStatusCounts(
        PayrollDeductionSummaryLockStatusCountsDto source) =>
    new(source.All, source.Open, source.Locked);
}
