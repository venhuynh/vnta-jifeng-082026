using Vnta.Hrm.Application.ChamCong.BangCongThang;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruBHXHYT;

public sealed class PayrollInsuranceDeductionDataProvider(
    IPayrollInsuranceDeductionReadService payrollInsuranceDeductionReadService,
    IPayrollInsuranceDeductionRefreshService payrollInsuranceDeductionRefreshService,
    IPayrollInsuranceDeductionManualAdjustmentService payrollInsuranceDeductionManualAdjustmentService,
    IPayrollInsuranceDeductionLockService payrollInsuranceDeductionLockService,
    IPayrollInsuranceDeductionLegacyWriteService payrollInsuranceDeductionLegacyWriteService,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory,
    IPayrollAdministrationAuthorizer payrollAdministrationAuthorizer,
    MonthlyWorkSummaryDataProvider monthlyWorkSummaryDataProvider)
{
    private const int MonthlyWorkScopeVerificationTake = 5000;
    private const int ExportBatchSize = 5000;

    public async Task<PayrollInsuranceDeductionLoadResult> SearchAsync(
        PayrollInsuranceDeductionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await payrollInsuranceDeductionReadService.SearchAsync(filter, cancellationToken);
        return new PayrollInsuranceDeductionLoadResult(
            page.Rows.Select(MapRecord).ToArray(),
            page.TotalCount);
    }

    public Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        CancellationToken cancellationToken = default) =>
        payrollInsuranceDeductionRefreshService.RefreshAsync(
            new RefreshPayrollInsuranceDeductionRequest(targetPayrollMonth, targetPayrollYear),
            cancellationToken);

    public Task<RefreshPayrollInsuranceDeductionResult> RefreshRowAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        Guid payrollDeductionSummaryRecordId,
        CancellationToken cancellationToken = default) =>
        payrollInsuranceDeductionRefreshService.RefreshAsync(
            new RefreshPayrollInsuranceDeductionRequest(
                targetPayrollMonth,
                targetPayrollYear,
                payrollDeductionSummaryRecordId),
            cancellationToken);

    // Employee, payroll period and row identifiers originate from the UI only as lookup hints.
    // The Interactive Server boundary demands payroll access, reloads the payroll row in the
    // requested period and derives the attendance date range only after that scope check.
    public async Task<MonthlyWorkSummaryGridRowRecord?> LoadEmployeeMonthlyWorkAsync(
        Guid insuranceDeductionRecordId,
        Guid payrollDeductionSummaryRecordId,
        Guid employeeId,
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default)
    {
        if (insuranceDeductionRecordId == Guid.Empty
            || payrollDeductionSummaryRecordId == Guid.Empty
            || employeeId == Guid.Empty
            || payrollYear is < 2000 or > 2100
            || payrollMonth is < 1 or > 12)
        {
            return null;
        }

        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);

        var page = await payrollInsuranceDeductionReadService.SearchAsync(
            new PayrollInsuranceDeductionFilter(
                payrollMonth,
                payrollYear,
                SearchText: null,
                Take: MonthlyWorkScopeVerificationTake),
            cancellationToken);
        var belongsToAppliedPeriod = page.Rows.Any(record =>
            record.Id == insuranceDeductionRecordId
            && record.PayrollDeductionSummaryRecordId == payrollDeductionSummaryRecordId
            && record.EmployeeId == employeeId
            && record.PayrollYear == payrollYear
            && record.PayrollMonth == payrollMonth);
        if (!belongsToAppliedPeriod)
        {
            return null;
        }

        var fromDate = new DateOnly(payrollYear, payrollMonth, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);
        return await monthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
            fromDate,
            toDate,
            employeeId,
            cancellationToken);
    }

    public async Task<PayrollInsuranceDeductionRecord> UpdateManualValuesAsync(
        UpdatePayrollInsuranceDeductionManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.PayrollInsuranceDeduction.ManualValuesUpdated,
            token => payrollInsuranceDeductionManualAdjustmentService.UpdateManualValuesAsync(request, token),
            AuditCaptureMode.OperationOnly,
            cancellationToken: cancellationToken);

        return MapRecord(result);
    }

    public async Task<PayrollInsuranceDeductionRecord> SetLockStateAsync(
        Guid payrollDeductionSummaryRecordId,
        bool isLocked,
        DateTime originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var result = await payrollInsuranceDeductionLockService.SetLockStateAsync(
            new SetPayrollInsuranceDeductionLockStateRequest(
                payrollDeductionSummaryRecordId,
                isLocked,
                originalUpdatedAtUtc),
            cancellationToken);

        return MapRecord(result);
    }

    public Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollInsuranceDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        payrollInsuranceDeductionLockService.SetLockStateBatchAsync(request, cancellationToken);

    public Task<string?> ValidateAsync(
        PayrollInsuranceDeductionRecord record,
        CancellationToken cancellationToken = default) =>
        payrollInsuranceDeductionLegacyWriteService.ValidateAsync(MapRequest(record), cancellationToken);

    public async Task<IReadOnlyList<PayrollInsuranceDeductionRecord>> SaveAsync(
        PayrollInsuranceDeductionRecord record,
        bool isNew,
        PayrollInsuranceDeductionFilter reloadFilter,
        CancellationToken cancellationToken = default)
    {
        await payrollInsuranceDeductionLegacyWriteService.SaveAsync(MapRequest(record), isNew, cancellationToken);
        return (await SearchAsync(reloadFilter, cancellationToken)).Rows;
    }

    public async Task<IReadOnlyList<PayrollInsuranceDeductionRecord>> LoadAllForPeriodExportAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default)
    {
        var records = new List<PayrollInsuranceDeductionRecord>();
        var skip = 0;

        while (true)
        {
            var page = await SearchAsync(
                new PayrollInsuranceDeductionFilter(
                    payrollMonth,
                    payrollYear,
                    SearchText: null,
                    Skip: skip,
                    Take: ExportBatchSize),
                cancellationToken);

            records.AddRange(page.Rows);
            skip += page.Rows.Count;

            if (skip >= page.TotalCount || page.Rows.Count == 0)
            {
                return records;
            }
        }
    }

    private static UpsertPayrollInsuranceDeductionRequest MapRequest(PayrollInsuranceDeductionRecord source) =>
        new()
        {
            Id = source.Id,
            PayrollDeductionSummaryRecordId = source.PayrollDeductionSummaryRecordId,
            EmployeeId = source.EmployeeId ?? Guid.Empty,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            InsuranceSalaryBaseAmount = source.InsuranceSalaryBaseAmount,
            SocialInsuranceRate = source.SocialInsuranceRate,
            HealthInsuranceRate = source.HealthInsuranceRate,
            UnemploymentInsuranceRate = source.UnemploymentInsuranceRate,
            IsParticipating = source.IsParticipating,
            ParticipationChangeType = source.ParticipationChangeType,
            EffectiveDate = source.EffectiveDate,
            IsLocked = source.IsLocked,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static PayrollInsuranceDeductionRecord MapRecord(PayrollInsuranceDeductionListItemDto source)
    {
        var record = new PayrollInsuranceDeductionRecord
        {
            Id = source.Id,
            PayrollDeductionSummaryRecordId = source.PayrollDeductionSummaryRecordId,
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            InsuranceSalaryBaseAmount = source.InsuranceSalaryBaseAmount,
            SocialInsuranceRate = source.SocialInsuranceRate,
            HealthInsuranceRate = source.HealthInsuranceRate,
            UnemploymentInsuranceRate = source.UnemploymentInsuranceRate,
            IsParticipating = source.IsParticipating,
            ParticipationChangeType = source.ParticipationChangeType,
            EffectiveDate = source.EffectiveDate,
            IsLocked = source.IsLocked,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

        record.SetServerCalculatedValues(
            source.TotalInsuranceRate,
            source.SocialInsuranceAmount,
            source.HealthInsuranceAmount,
            source.UnemploymentInsuranceAmount,
            source.TotalDeductionAmount);
        return record;
    }
}

public sealed record PayrollInsuranceDeductionLoadResult(
    IReadOnlyList<PayrollInsuranceDeductionRecord> Rows,
    int TotalCount);
