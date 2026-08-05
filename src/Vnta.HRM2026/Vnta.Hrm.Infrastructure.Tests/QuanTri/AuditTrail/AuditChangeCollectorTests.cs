using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.CaKip.LichLamViec;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.CaKip.CaiDatCa;
using Vnta.Hrm.Infrastructure.CaKip.LichLamViec;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.QuanTri.AuditTrail;

public sealed class AuditChangeCollectorTests
{
    [Fact]
    public void Collect_records_allowlisted_shift_add_and_removes_sensitive_metadata()
    {
        using var dbContext = CreateDbContext();
        var shift = CreateShift("Morning shift");
        dbContext.Shifts.Add(shift);

        var auditEvents = CreateCollector().Collect(
            dbContext.ChangeTracker,
            CreateCommand(new Dictionary<string, string>
            {
                ["reason"] = "manual setup",
                ["requestId"] = "request-123",
                ["deviceToken"] = "must-not-be-recorded",
                ["Password"] = "must-not-be-recorded"
            }));

        var auditEvent = Assert.Single(auditEvents);
        Assert.Equal(AuditEntityTypes.Shift, auditEvent.EntityType);
        Assert.Equal(shift.Id.ToString("D"), auditEvent.EntityId);
        Assert.Equal(shift.Name, auditEvent.EntityDisplayName);
        Assert.Contains(auditEvent.PropertyChanges, change => change.PropertyName == nameof(AttendanceShiftRow.Code));
        Assert.Contains(auditEvent.PropertyChanges, change => change.PropertyName == nameof(AttendanceShiftRow.Name));
        Assert.DoesNotContain(auditEvent.PropertyChanges, change => change.PropertyName == nameof(AttendanceShiftRow.Description));
        Assert.All(auditEvent.PropertyChanges, change => Assert.Equal(auditEvent.Id, change.AuditEventId));

        using var metadata = JsonDocument.Parse(auditEvent.MetadataJson!);
        Assert.Equal("manual setup", metadata.RootElement.GetProperty("reason").GetString());
        Assert.Equal("request-123", metadata.RootElement.GetProperty("requestId").GetString());
        Assert.False(metadata.RootElement.TryGetProperty("deviceToken", out _));
        Assert.False(metadata.RootElement.TryGetProperty("Password", out _));
    }

    [Fact]
    public void Collect_records_only_changed_allowlisted_shift_properties_with_old_and_new_values()
    {
        using var dbContext = CreateDbContext();
        var shift = CreateShift("Morning shift");
        dbContext.Attach(shift);
        shift.Name = "Evening shift";
        dbContext.Entry(shift).Property(row => row.Name).IsModified = true;

        var auditEvent = Assert.Single(CreateCollector().Collect(dbContext.ChangeTracker, CreateCommand()));
        var change = Assert.Single(auditEvent.PropertyChanges);

        Assert.Equal(nameof(AttendanceShiftRow.Name), change.PropertyName);
        Assert.Equal("\"Morning shift\"", change.OldValueJson);
        Assert.Equal("\"Evening shift\"", change.NewValueJson);
        Assert.Equal("Morning shift", change.OldDisplay);
        Assert.Equal("Evening shift", change.NewDisplay);
    }

    [Fact]
    public void Collect_allows_a_hard_delete_snapshot_for_work_calendar_days()
    {
        using var dbContext = CreateDbContext();
        var calendarDay = new AttendanceWorkCalendarDayRow
        {
            Id = Guid.NewGuid(),
            WorkDate = new DateOnly(2026, 7, 17),
            DayType = AttendanceWorkCalendarDayType.Holiday,
            Name = "Company holiday",
            Note = "Annual observance",
            CreatedAtUtc = new DateTime(2026, 1, 1)
        };
        dbContext.Attach(calendarDay);
        dbContext.Remove(calendarDay);

        var auditEvent = Assert.Single(CreateCollector().Collect(dbContext.ChangeTracker, CreateCommand()));

        Assert.Equal(AuditEntityTypes.WorkCalendarDay, auditEvent.EntityType);
        Assert.Equal(calendarDay.Id.ToString("D"), auditEvent.EntityId);
        Assert.Equal("2026-07-17 - Company holiday", auditEvent.EntityDisplayName);
        Assert.Contains(auditEvent.PropertyChanges, change => change.PropertyName == nameof(AttendanceWorkCalendarDayRow.WorkDate));
        Assert.Contains(auditEvent.PropertyChanges, change => change.PropertyName == nameof(AttendanceWorkCalendarDayRow.DayType));
        Assert.Contains(auditEvent.PropertyChanges, change => change.PropertyName == nameof(AttendanceWorkCalendarDayRow.Name));
        Assert.Contains(auditEvent.PropertyChanges, change => change.PropertyName == nameof(AttendanceWorkCalendarDayRow.Note));
        Assert.All(auditEvent.PropertyChanges, change =>
        {
            Assert.NotNull(change.OldValueJson);
            Assert.Null(change.NewValueJson);
            Assert.Null(change.NewDisplay);
        });
    }

    [Fact]
    public void Collect_does_not_allow_hard_deletes_for_shifts()
    {
        using var dbContext = CreateDbContext();
        var shift = CreateShift("Morning shift");
        dbContext.Attach(shift);
        dbContext.Remove(shift);

        var auditEvents = CreateCollector().Collect(dbContext.ChangeTracker, CreateCommand());

        Assert.Empty(auditEvents);
    }

    [Fact]
    public void Collect_excludes_entities_that_are_not_in_the_audit_allow_list()
    {
        using var dbContext = CreateDbContext();
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = Guid.NewGuid(),
            PositionId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            EmployeeCode = "EMP-001",
            FirstName = "Audit",
            LastName = "Excluded",
            HireDate = new DateTime(2026, 1, 1),
            CreatedAtUtc = new DateTime(2026, 1, 1)
        });

        var auditEvents = CreateCollector().Collect(dbContext.ChangeTracker, CreateCommand());

        Assert.Empty(auditEvents);
    }

    [Fact]
    public void Collect_masks_values_when_the_property_policy_is_sensitive()
    {
        using var dbContext = CreateDbContext();
        dbContext.Shifts.Add(CreateShift("Sensitive shift"));

        var policy = new AuditEntityPolicy(
            "SensitiveShift",
            allowHardDelete: false,
            new Dictionary<string, AuditPropertyPolicy>
            {
                [nameof(AttendanceShiftRow.Name)] = new("Shift name", IsSensitive: true)
            });
        var collector = new AuditChangeCollector(new SingleEntityPolicy(typeof(AttendanceShiftRow), policy));

        var auditEvent = Assert.Single(collector.Collect(dbContext.ChangeTracker, CreateCommand()));
        var change = Assert.Single(auditEvent.PropertyChanges);

        Assert.True(change.IsSensitive);
        Assert.Null(change.OldValueJson);
        Assert.Null(change.NewValueJson);
        Assert.Null(change.OldDisplay);
        Assert.Equal("Changed", change.NewDisplay);
    }

    [Fact]
    public void Collect_records_seniority_allowance_changes_and_masks_the_amount()
    {
        using var dbContext = CreateDbContext();
        var row = new PayrollEmployeeSeniorityAllowanceRow
        {
            PayrollAllowanceSummaryRecordId = Guid.NewGuid(),
            AllowanceAmount = 200_000m,
            Note = "Điều chỉnh theo phê duyệt",
            IsLocked = false,
            CreatedAtUtc = new DateTime(2026, 7, 18),
            CreatedBy = "test-user"
        };
        dbContext.PayrollEmployeeSeniorityAllowances.Add(row);

        var auditEvent = Assert.Single(CreateCollector().Collect(
            dbContext.ChangeTracker,
            CreateCommand() with { ActionIntent = AuditActions.SeniorityAllowance.ManualValueUpdated }));

        Assert.Equal(AuditEntityTypes.SeniorityAllowance, auditEvent.EntityType);
        Assert.Equal(row.PayrollAllowanceSummaryRecordId.ToString("D"), auditEvent.EntityId);

        var amountChange = Assert.Single(auditEvent.PropertyChanges,
            change => change.PropertyName == nameof(PayrollEmployeeSeniorityAllowanceRow.AllowanceAmount));
        Assert.True(amountChange.IsSensitive);
        Assert.Null(amountChange.NewValueJson);
        Assert.Equal("Changed", amountChange.NewDisplay);
    }

    [Fact]
    public void Collect_records_hazard_manual_values_and_masks_allowance_amount()
    {
        using var dbContext = CreateDbContext();
        var row = new PayrollHazardAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = Guid.NewGuid(),
            QualifiedWorkdayCount = 26m,
            LateEarlyDeductionDays = 0m,
            PayableWorkdayCount = 26m,
            HazardAllowancePerDay = 23_077m,
            HazardAllowanceAmount = 600_000m,
            IsEligibleDepartment = true,
            CreatedAtUtc = new DateTime(2026, 7, 18),
            CreatedBy = "test-user"
        };
        dbContext.PayrollHazardAllowanceRecords.Add(row);

        var auditEvent = Assert.Single(CreateCollector().Collect(
            dbContext.ChangeTracker,
            CreateCommand() with { ActionIntent = AuditActions.HazardAllowance.ManualValuesUpdated }));

        Assert.Equal(AuditEntityTypes.HazardAllowance, auditEvent.EntityType);
        Assert.Equal(row.PayrollAllowanceSummaryRecordId.ToString("D"), auditEvent.EntityId);
        var amountChange = Assert.Single(auditEvent.PropertyChanges,
            change => change.PropertyName == nameof(PayrollHazardAllowanceRecordRow.HazardAllowanceAmount));
        Assert.True(amountChange.IsSensitive);
        Assert.Equal("Changed", amountChange.NewDisplay);
    }

    [Fact]
    public void Collect_records_hazard_lock_on_the_summary_aggregate()
    {
        using var dbContext = CreateDbContext();
        var summary = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            IsLocked = false
        };
        dbContext.Attach(summary);
        summary.IsLocked = true;
        dbContext.Entry(summary).Property(row => row.IsLocked).IsModified = true;

        var auditEvent = Assert.Single(CreateCollector().Collect(
            dbContext.ChangeTracker,
            CreateCommand() with { ActionIntent = AuditActions.HazardAllowance.LockStateChanged }));

        Assert.Equal(AuditEntityTypes.AllowanceSummary, auditEvent.EntityType);
        Assert.Equal(summary.Id.ToString("D"), auditEvent.EntityId);
        Assert.Contains(auditEvent.PropertyChanges,
            change => change.PropertyName == nameof(PayrollAllowanceSummaryRecordRow.IsLocked));
    }

    [Fact]
    public void Collect_records_other_allowance_change_and_keeps_the_amount_sensitive()
    {
        using var dbContext = CreateDbContext();
        var row = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = Guid.NewGuid(),
            AllowanceName = "Hỗ trợ ăn ca",
            IsFixedAmount = true,
            AllowanceAmount = 500_000m,
            Note = "Theo quy chế",
            CreatedAtUtc = new DateTime(2026, 7, 30),
            CreatedBy = "test-user"
        };
        dbContext.PayrollOtherAllowanceRecords.Add(row);

        var auditEvent = Assert.Single(CreateCollector().Collect(
            dbContext.ChangeTracker,
            CreateCommand() with { ActionIntent = AuditActions.OtherAllowance.Created }));

        Assert.Equal(AuditEntityTypes.OtherAllowance, auditEvent.EntityType);
        Assert.Equal(row.Id.ToString("D"), auditEvent.EntityId);
        Assert.Equal(row.AllowanceName, auditEvent.EntityDisplayName);
        var amountChange = Assert.Single(auditEvent.PropertyChanges,
            change => change.PropertyName == nameof(PayrollOtherAllowanceRecordRow.AllowanceAmount));
        Assert.True(amountChange.IsSensitive);
        Assert.Null(amountChange.NewValueJson);
        Assert.Equal("Changed", amountChange.NewDisplay);
    }

    [Fact]
    public void Collect_records_other_allowance_hard_delete_snapshot()
    {
        using var dbContext = CreateDbContext();
        var row = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = Guid.NewGuid(),
            AllowanceName = "Hỗ trợ điện thoại",
            IsFixedAmount = true,
            AllowanceAmount = 300_000m,
            CreatedAtUtc = new DateTime(2026, 7, 30),
            CreatedBy = "test-user"
        };
        dbContext.Attach(row);
        dbContext.Remove(row);

        var auditEvent = Assert.Single(CreateCollector().Collect(
            dbContext.ChangeTracker,
            CreateCommand() with { ActionIntent = AuditActions.OtherAllowance.Deleted }));

        Assert.Equal(AuditEntityTypes.OtherAllowance, auditEvent.EntityType);
        Assert.Contains(auditEvent.PropertyChanges,
            change => change.PropertyName == nameof(PayrollOtherAllowanceRecordRow.AllowanceName)
                && change.OldValueJson is not null
                && change.NewValueJson is null);
    }

    private static AuditChangeCollector CreateCollector() => new(new AuditPolicy());

    private static AuditCommand CreateCommand(IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            Guid.NewGuid(),
            AuditActions.Shift.Save,
            new AuditActor("test-user", "Test User", AuditActorKind.User, AuditSource.InteractiveServer),
            "collector-test-correlation",
            Metadata: metadata);

    private static AttendanceShiftRow CreateShift(string name) => new()
    {
        Id = Guid.NewGuid(),
        Code = "SHIFT-01",
        Name = name,
        DepartmentGroup = "Factory",
        StartTime = "08:00",
        EndTime = "17:00",
        IsOvernight = false,
        BreakStartTime = "12:00",
        BreakEndTime = "13:00",
        Status = 1,
        ColorHex = "#0066CC",
        WorkingDays = "1,2,3,4,5",
        CreatedAtUtc = new DateTime(2026, 1, 1)
    };

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=vnta_audit_unit_tests;Username=test;Password=test")
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class SingleEntityPolicy(Type entityType, AuditEntityPolicy policy) : IAuditPolicy
    {
        public AuditEntityPolicy? GetPolicy(Type candidate) =>
            candidate == entityType ? policy : null;
    }
}
