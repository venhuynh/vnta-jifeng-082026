using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemKhac;

public sealed class OtherResponsibilityAllowanceDataProvider(
    IOtherResponsibilityAllowanceReadService readService,
    IOtherResponsibilityAllowancePeriodPreparationService periodPreparationService,
    IOtherResponsibilityAllowanceRecalculationService recalculationService,
    IOtherResponsibilityAllowanceLockService lockService) : IOtherResponsibilityAllowanceDataProvider
{
    public Task PreparePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default) =>
        periodPreparationService.PreparePeriodAsync(payrollYear, payrollMonth, requestedBy: null, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<OtherResponsibilityAllowanceRecord>> SearchAsync(
        OtherResponsibilityAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = await readService.SearchAsync(filter, cancellationToken);
        return result.Select(MapRecord).ToArray();
    }

    public Task<RecalculateOtherResponsibilityAllowanceResult> RecalculateAsync(
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default) =>
        recalculationService.RecalculateAsync(
            new RecalculateOtherResponsibilityAllowanceRequest(payrollYear, payrollMonth),
            cancellationToken: cancellationToken);

    /// <summary>
    /// Khóa hoặc mở khóa theo scope đã xác nhận. Collection null là toàn bộ kỳ; collection rỗng
    /// vẫn được gửi là selected scope để backend từ chối, không được mở rộng thành toàn kỳ.
    /// </summary>
    public Task<SetOtherResponsibilityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        int payrollYear,
        int payrollMonth,
        bool isLocked,
        IReadOnlyCollection<OtherResponsibilityAllowanceRecord>? records,
        CancellationToken cancellationToken = default)
    {
        var distinctRecords = records?
            .Where(record => record.PayrollAllowanceSummaryRecordId != Guid.Empty)
            .GroupBy(record => record.PayrollAllowanceSummaryRecordId)
            .Select(group => group.First())
            .ToArray();

        return lockService.SetLockStateBatchAsync(
            new SetOtherResponsibilityAllowanceBatchLockStateRequest(
                payrollYear,
                payrollMonth,
                isLocked,
                PayrollAllowanceSummaryRecordIds: distinctRecords?
                    .Select(record => record.PayrollAllowanceSummaryRecordId)
                    .ToArray(),
                ConcurrencyTokens: distinctRecords?
                    .Select(record => new OtherResponsibilityAllowanceLockStateConcurrencyToken(
                        record.PayrollAllowanceSummaryRecordId,
                        record.UpdatedAtUtc))
                    .ToArray()),
            cancellationToken: cancellationToken);
    }

    private static OtherResponsibilityAllowanceRecord MapRecord(OtherResponsibilityAllowanceListItemDto source) =>
        new()
        {
            Id = source.Id,
            PayrollAllowanceSummaryRecordId = source.PayrollAllowanceSummaryRecordId,
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            AllowanceWorkdayCount = source.AllowanceWorkdayCount,
            StandardResponsibilityAllowanceAmount = source.StandardResponsibilityAllowanceAmount,
            ActualResponsibilityAllowanceAmount = source.ActualResponsibilityAllowanceAmount,
            Note = source.Note,
            IsLocked = source.IsLocked,
            RefreshedAtUtc = source.RefreshedAtUtc,
            RefreshedBy = source.RefreshedBy,
            CreatedAtUtc = source.CreatedAtUtc,
            CreatedBy = source.CreatedBy,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedBy = source.UpdatedBy
        };
}
