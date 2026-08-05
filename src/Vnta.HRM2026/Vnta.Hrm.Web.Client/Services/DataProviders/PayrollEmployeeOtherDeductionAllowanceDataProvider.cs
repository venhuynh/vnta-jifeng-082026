using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruKhac;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class PayrollEmployeeOtherDeductionAllowanceDataProvider(
    IPayrollEmployeeOtherDeductionAllowanceService otherDeductionService)
{
    private const int ExportPageSize = 5000;

    public Task PreparePeriodAsync(int year, int month, CancellationToken cancellationToken = default) =>
        otherDeductionService.PreparePeriodAsync(year, month, cancellationToken);

    public async Task<IReadOnlyList<KhauTruKhacRecord>> SearchAsync(
        PayrollEmployeeOtherDeductionAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        (await otherDeductionService.SearchAsync(filter, cancellationToken)).Select(MapRecord).ToArray();

    public async Task<IReadOnlyList<KhauTruKhacRecord>> LoadAllForPeriodExportAsync(
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default)
    {
        var page = await otherDeductionService.SearchPageAsync(
            new PayrollEmployeeOtherDeductionAllowanceFilter(payrollMonth, payrollYear, Take: ExportPageSize, Skip: 0),
            cancellationToken);
        return page.Rows.Select(MapRecord).ToArray();
    }

    public Task<RefreshPayrollEmployeeOtherDeductionAllowanceResult> RefreshAsync(
        RefreshPayrollEmployeeOtherDeductionAllowanceRequest request,
        CancellationToken cancellationToken = default) =>
        otherDeductionService.RefreshAsync(request, cancellationToken);

    public async Task<KhauTruKhacRecord> UpdateManualValuesAsync(
        Guid payrollDeductionSummaryRecordId,
        decimal deductionAmount,
        string? note,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default) =>
        MapRecord(await otherDeductionService.UpdateManualValuesAsync(
            new UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest(
                payrollDeductionSummaryRecordId,
                deductionAmount,
                note,
                originalUpdatedAtUtc),
            cancellationToken));

    public async Task<KhauTruKhacRecord> SetLockStateAsync(
        Guid payrollDeductionSummaryRecordId,
        bool isLocked,
        CancellationToken cancellationToken = default) =>
        MapRecord(await otherDeductionService.SetLockStateAsync(
            new SetPayrollEmployeeOtherDeductionAllowanceLockStateRequest(payrollDeductionSummaryRecordId, isLocked),
            cancellationToken));

    public Task<SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        otherDeductionService.SetLockStateBatchAsync(request, cancellationToken);

    private static KhauTruKhacRecord MapRecord(PayrollEmployeeOtherDeductionAllowanceListItemDto source) => new()
    {
        Id = source.Id,
        PayrollDeductionSummaryRecordId = source.PayrollDeductionSummaryRecordId,
        EmployeeId = source.EmployeeId,
        EmployeeCode = source.EmployeeCode,
        EmployeeName = source.EmployeeName,
        DepartmentName = source.DepartmentName,
        PositionName = source.PositionName,
        PayrollMonth = (short)source.PayrollMonth,
        PayrollYear = (short)source.PayrollYear,
        Description = source.Description,
        DeductionAmount = source.DeductionAmount,
        Note = source.Note,
        IsLocked = source.IsLocked,
        UpdatedAtUtc = source.UpdatedAtUtc,
        VersionAtUtc = source.UpdatedAtUtc ?? source.CreatedAtUtc,
        RefreshedBy = null
    };
}
