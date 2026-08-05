using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Models;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Export;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

/// <summary>
/// Boundary TNCN riêng cho màn khấu trừ. Row UI tương thích chỉ dùng để render
/// danh sách hiện hữu; mọi ghi nhận điều chỉnh đi qua contract Thuế TNCN riêng.
/// </summary>
public sealed class PayrollPersonalIncomeTaxDeductionDataProvider(
    IPayrollPersonalIncomeTaxDeductionReadService readService,
    IPayrollPersonalIncomeTaxDeductionRefreshService refreshService,
    IPayrollPersonalIncomeTaxDeductionManualAdjustmentService manualAdjustmentService,
    IPayrollPersonalIncomeTaxDeductionLockService lockService)
{
    private const int MaximumPageSize = 2000;

    public async Task<IReadOnlyList<PayrollPersonalIncomeTaxDeductionRecord>> SearchAsync(
        int payrollMonth,
        int payrollYear,
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        var page = await readService.SearchAsync(
            new PayrollPersonalIncomeTaxDeductionFilter(
                payrollMonth,
                payrollYear,
                searchText,
                Skip: 0,
                Take: MaximumPageSize),
            cancellationToken);
        return page.Items.Select(MapRecord).ToArray();
    }

    public async Task<PayrollPersonalIncomeTaxDeductionRecord> UpdateManualValueAsync(
        Guid payrollDeductionSummaryRecordId,
        decimal deductionAmount,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var updated = await manualAdjustmentService.UpdateManualValueAsync(
            new UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest(
                payrollDeductionSummaryRecordId,
                deductionAmount,
                originalUpdatedAtUtc),
            cancellationToken);
        return MapRecord(updated);
    }

    public async Task<RefreshPayrollPersonalIncomeTaxDeductionResult> RefreshAsync(
        Guid payrollDeductionSummaryRecordId,
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default)
    {
        return await refreshService.RefreshAsync(
            new RefreshPayrollPersonalIncomeTaxDeductionRequest(
                payrollYear,
                payrollMonth,
                payrollDeductionSummaryRecordId),
            cancellationToken);
    }

    public async Task<SetPayrollPersonalIncomeTaxDeductionBatchLockStateResult> SetLockStateBatchAsync(
        int payrollYear,
        int payrollMonth,
        bool isLocked,
        PayrollPersonalIncomeTaxDeductionLockActionScope scope,
        IReadOnlyList<Guid>? payrollDeductionSummaryRecordIds,
        CancellationToken cancellationToken = default)
    {
        return await lockService.SetLockStateBatchAsync(
            new SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest(
                payrollYear,
                payrollMonth,
                isLocked,
                scope,
                payrollDeductionSummaryRecordIds),
            cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollPersonalIncomeTaxDeductionExportRow>> ExportAsync(
        int payrollYear,
        int payrollMonth,
        PayrollPersonalIncomeTaxDeductionExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var records = await SearchAsync(payrollMonth, payrollYear, null, cancellationToken);
        return records.Select(record => new PayrollPersonalIncomeTaxDeductionExportRow(
            record.EmployeeCode,
            record.EmployeeName,
            record.DepartmentName,
            record.PositionName,
            record.PayrollPeriodDisplay,
            record.DeductionAmount,
            record.LockStatusText)).ToArray();
    }

    private static PayrollPersonalIncomeTaxDeductionRecord MapRecord(PayrollPersonalIncomeTaxDeductionListItemDto source)
    {
        return new PayrollPersonalIncomeTaxDeductionRecord(
            source.PayrollDeductionSummaryRecordId,
            source.EmployeeId,
            source.EmployeeCode ?? string.Empty,
            source.EmployeeName ?? string.Empty,
            source.DepartmentName ?? string.Empty,
            source.PositionName ?? string.Empty,
            source.PayrollMonth,
            source.PayrollYear,
            source.DeductionAmount,
            source.IsSummaryLocked || source.IsLocked,
            source.CreatedAtUtc,
            source.UpdatedAtUtc);
    }
}
