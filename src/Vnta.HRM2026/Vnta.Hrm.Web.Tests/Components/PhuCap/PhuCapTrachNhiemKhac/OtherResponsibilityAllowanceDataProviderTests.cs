using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemKhac;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapTrachNhiemKhac;

public sealed class OtherResponsibilityAllowanceDataProviderTests
{
    [Fact]
    public async Task SearchAsync_maps_the_calculated_snapshot_and_audit_version_to_the_ui_record_without_recalculation()
    {
        var updatedAt = new DateTime(2026, 6, 30, 8, 30, 0, DateTimeKind.Utc);
        var source = new OtherResponsibilityAllowanceListItemDto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "NV-001", "Nguyen Van A", "Production", "Operator",
            6, 2026, 24.125m, 1_500_000m, 1_333_333.33m, "calculated", false,
            updatedAt.AddMinutes(-5), "calculator", updatedAt.AddDays(-1), "creator", updatedAt, "updater");
        var readService = new StubReadService([source]);
        var provider = new OtherResponsibilityAllowanceDataProvider(
            readService,
            new StubPreparationService(),
            new StubRecalculationService(),
            new CapturingLockService());
        var filter = new OtherResponsibilityAllowanceFilter(6, 2026, "NV-001", Take: 50);

        var record = Assert.Single(await provider.SearchAsync(filter));

        Assert.Equal(filter, readService.Filter);
        Assert.Equal(source.Id, record.Id);
        Assert.Equal(source.PayrollAllowanceSummaryRecordId, record.PayrollAllowanceSummaryRecordId);
        Assert.Equal(source.EmployeeId, record.EmployeeId);
        Assert.Equal(source.EmployeeCode, record.EmployeeCode);
        Assert.Equal(source.EmployeeName, record.EmployeeName);
        Assert.Equal(source.DepartmentName, record.DepartmentName);
        Assert.Equal(source.PositionName, record.PositionName);
        Assert.Equal(source.AllowanceWorkdayCount, record.AllowanceWorkdayCount);
        Assert.Equal(source.StandardResponsibilityAllowanceAmount, record.StandardResponsibilityAllowanceAmount);
        Assert.Equal(source.ActualResponsibilityAllowanceAmount, record.ActualResponsibilityAllowanceAmount);
        Assert.Equal(source.Note, record.Note);
        Assert.Equal(source.IsLocked, record.IsLocked);
        Assert.Equal(source.UpdatedAtUtc, record.UpdatedAtUtc);
        Assert.Equal(source.UpdatedBy, record.UpdatedBy);
    }

    [Fact]
    public async Task SetLockStateBatchAsync_preserves_the_difference_between_whole_period_and_selected_rows_and_uses_the_displayed_version()
    {
        var lockService = new CapturingLockService();
        var provider = new OtherResponsibilityAllowanceDataProvider(
            new StubReadService([]),
            new StubPreparationService(),
            new StubRecalculationService(),
            lockService);
        var firstId = Guid.NewGuid();
        var updatedAt = new DateTime(2026, 6, 30, 8, 30, 0, DateTimeKind.Utc);
        var selected = new OtherResponsibilityAllowanceRecord
        {
            PayrollAllowanceSummaryRecordId = firstId,
            UpdatedAtUtc = updatedAt
        };

        await provider.SetLockStateBatchAsync(2026, 6, true, null);
        Assert.Null(lockService.Request!.PayrollAllowanceSummaryRecordIds);
        Assert.Null(lockService.Request.ConcurrencyTokens);

        await provider.SetLockStateBatchAsync(2026, 6, true, [selected, selected, new OtherResponsibilityAllowanceRecord()]);

        Assert.Equal([firstId], lockService.Request!.PayrollAllowanceSummaryRecordIds);
        var token = Assert.Single(lockService.Request.ConcurrencyTokens!);
        Assert.Equal(firstId, token.PayrollAllowanceSummaryRecordId);
        Assert.Equal(updatedAt, token.OriginalUpdatedAtUtc);
    }

    private sealed class StubReadService(IReadOnlyList<OtherResponsibilityAllowanceListItemDto> result)
        : IOtherResponsibilityAllowanceReadService
    {
        public OtherResponsibilityAllowanceFilter? Filter { get; private set; }

        public Task<IReadOnlyList<OtherResponsibilityAllowanceListItemDto>> SearchAsync(
            OtherResponsibilityAllowanceFilter filter,
            CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult(result);
        }
    }

    private sealed class StubPreparationService : IOtherResponsibilityAllowancePeriodPreparationService
    {
        public Task PreparePeriodAsync(int year, int month, string? requestedBy, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubRecalculationService : IOtherResponsibilityAllowanceRecalculationService
    {
        public Task<RecalculateOtherResponsibilityAllowanceResult> RecalculateAsync(
            RecalculateOtherResponsibilityAllowanceRequest request,
            string? requestedBy = null,
            CancellationToken cancellationToken = default) => Task.FromResult(new RecalculateOtherResponsibilityAllowanceResult(0, 0));
    }

    private sealed class CapturingLockService : IOtherResponsibilityAllowanceLockService
    {
        public SetOtherResponsibilityAllowanceBatchLockStateRequest? Request { get; private set; }

        public Task<SetOtherResponsibilityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
            SetOtherResponsibilityAllowanceBatchLockStateRequest request,
            string? requestedBy = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new SetOtherResponsibilityAllowanceBatchLockStateResult(
                request.PayrollYear,
                request.PayrollMonth,
                request.PayrollAllowanceSummaryRecordIds?.Count ?? 0,
                0));
        }
    }
}
