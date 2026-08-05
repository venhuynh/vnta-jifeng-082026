using Vnta.Hrm.Application.PhuCap.Common;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapChuyenCan;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class AttendanceAllowanceResultDataProviderTests
{
    [Fact]
    public async Task SearchPage_maps_server_calculated_attendance_fields_to_the_ui_record()
    {
        var source = new AttendanceAllowanceResultListItemDto(
            Guid.NewGuid(), PayrollAllowanceKind.Attendance, Guid.NewGuid(), "NV001", "Nguyen Van A", "Workshop", "Operator",
            7, 2026, 600_000m, 26m, 24.5m, 0.9423m, 300_000m, true, DateTime.UnixEpoch, DateTime.UnixEpoch,
            "attendance-ratio", AttendanceAllowanceClass.B, 24.5m, 15, 1.5m, true, 25m, 0.5m);
        var readService = new CapturingReadService(source);
        var provider = new AttendanceAllowanceResultDataProvider(
            readService, new UnsupportedExportService(), new UnsupportedRefreshService(), new UnsupportedManualService(),
            new UnsupportedWorkdayAdjustmentService(), new UnsupportedLockService(), new PassthroughAuditScopeFactory());
        var filter = new AttendanceAllowanceResultFilter(PayrollAllowanceKind.Attendance, 7, 2026, "NV001", Take: 20, Skip: 0);

        var page = await provider.SearchPageAsync(filter);

        var record = Assert.Single(page.Rows);
        Assert.Equal(filter, readService.Filter);
        Assert.Equal(source.Id, record.Id);
        Assert.Equal(source.ActualAllowanceAmount, record.ActualAllowanceAmount);
        Assert.Equal(source.AttendanceRate, record.AttendanceRate);
        Assert.Equal(source.AttendanceClass, record.AttendanceClass);
        Assert.Equal(source.CtlWorkdayCount, record.CtlWorkdayCount);
        Assert.Equal(source.LateEarlyMinutes, record.LateEarlyMinutes);
        Assert.Equal(source.Kqcc, record.Kqcc);
        Assert.True(record.HasKpViolation);
        Assert.Equal(source.AdministrativeWorkdayCount, record.AdministrativeWorkdayCount);
        Assert.Equal(source.LateEarlyDeductionDays, record.LateEarlyDeductionDays);
        Assert.True(record.IsLocked);
        Assert.Equal(3, page.OpenCount);
        Assert.Equal(2, page.LockedCount);
    }

    [Fact]
    public async Task UpdateWorkdays_forwards_the_editable_pair_as_one_atomic_command()
    {
        var commandService = new CapturingWorkdayAdjustmentService();
        var provider = new AttendanceAllowanceResultDataProvider(
            new CapturingReadService(CreateRow(Guid.NewGuid())),
            new UnsupportedExportService(),
            new UnsupportedRefreshService(),
            new UnsupportedManualService(),
            commandService,
            new UnsupportedLockService(),
            new PassthroughAuditScopeFactory());
        var id = Guid.NewGuid();
        var originalVersion = DateTime.UtcNow;

        await provider.UpdateWorkdaysAsync(id, 24m, 26m, originalVersion);

        Assert.Equal(new UpdateAttendanceAllowanceWorkdaysRequest(id, 24m, 26m, originalVersion), commandService.Request);
    }

    private sealed class CapturingReadService(AttendanceAllowanceResultListItemDto row) : IAttendanceAllowanceReadService
    {
        public AttendanceAllowanceResultFilter? Filter { get; private set; }
        public Task<AttendanceAllowanceRuleDto> GetRuleAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AttendanceAllowanceRuleDto([]));
        public Task<AttendanceAllowanceResultPageDto> SearchPageAsync(AttendanceAllowanceResultFilter filter, CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult(new AttendanceAllowanceResultPageDto([row], 5, 3, 2, 1, 2, 2, 5, 3, 1, 1));
        }
    }

    private sealed class PassthroughAuditScopeFactory : IInteractiveAuditCommandScopeFactory
    {
        public Task ExecuteAsync(string actionIntent, Func<CancellationToken, Task> command, AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) => command(cancellationToken);
        public Task<T> ExecuteAsync<T>(string actionIntent, Func<CancellationToken, Task<T>> command, AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) => command(cancellationToken);
    }

    private sealed class UnsupportedExportService : IAttendanceAllowanceExportService
    {
        public Task<IReadOnlyList<AttendanceAllowanceExportRowDto>> ExportAsync(AttendanceAllowanceExportRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnsupportedRefreshService : IAttendanceAllowanceRefreshService
    {
        public Task<RefreshAttendanceAllowanceResult> RefreshAsync(RefreshAttendanceAllowanceRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnsupportedManualService : IAttendanceAllowanceManualAdjustmentService
    {
        public Task<AttendanceAllowanceResultListItemDto> UpdateActualWorkdayAsync(UpdateAttendanceAllowanceActualWorkdayRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AttendanceAllowanceResultListItemDto> UpdateStandardWorkdayAsync(UpdateAttendanceAllowanceStandardWorkdayRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnsupportedWorkdayAdjustmentService : IAttendanceAllowanceWorkdayAdjustmentService
    {
        public Task<AttendanceAllowanceResultListItemDto> UpdateWorkdaysAsync(
            UpdateAttendanceAllowanceWorkdaysRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class CapturingWorkdayAdjustmentService : IAttendanceAllowanceWorkdayAdjustmentService
    {
        public UpdateAttendanceAllowanceWorkdaysRequest? Request { get; private set; }

        public Task<AttendanceAllowanceResultListItemDto> UpdateWorkdaysAsync(
            UpdateAttendanceAllowanceWorkdaysRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(CreateRow(request.Id));
        }
    }

    private sealed class UnsupportedLockService : IAttendanceAllowanceLockService
    {
        public Task<AttendanceAllowanceResultListItemDto> SetLockStateAsync(SetAttendanceAllowanceLockStateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SetAttendanceAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetAttendanceAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static AttendanceAllowanceResultListItemDto CreateRow(Guid id) => new(
        id,
        PayrollAllowanceKind.Attendance,
        Guid.NewGuid(),
        "NV001",
        "Nguyen Van A",
        null,
        null,
        7,
        2026,
        600_000m,
        26m,
        24m,
        0.9231m,
        300_000m,
        false,
        DateTime.UnixEpoch,
        DateTime.UnixEpoch,
        AttendanceClass: AttendanceAllowanceClass.B);
}
