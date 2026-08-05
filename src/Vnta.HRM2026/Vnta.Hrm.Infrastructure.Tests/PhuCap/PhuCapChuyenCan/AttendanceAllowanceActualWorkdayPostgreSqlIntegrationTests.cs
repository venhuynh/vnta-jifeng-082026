using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Application.CaKip.LichLamViec;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapChuyenCan;

/// <summary>
/// Regression tests for the manual-adjustment dialog. The dialog submits the
/// summary-record identifier and the detail timestamp for optimistic concurrency.
/// </summary>
[Collection(AttendanceAllowancePostgreSqlCollection.Name)]
public sealed class AttendanceAllowanceActualWorkdayPostgreSqlIntegrationTests(
    AttendanceAllowancePostgreSqlFixture fixture)
{
    [PostgreSqlAttendanceAllowanceFact]
    public async Task Save_actual_workday_from_edit_popup_persists_the_adjustment()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);

        await using var context = fixture.CreateDbContext();
        var auditScope = new AsyncLocalAuditScope();
        var service = new DatabaseAttendanceAllowanceManualAdjustmentService(
            context, auditScope, new AttendanceAllowanceCalculationPolicy(), new AttendanceAllowanceRequestValidator(), new AttendanceAllowanceWorkdayAdjustmentPolicy());

        var result = await service.UpdateActualWorkdayAsync(
            new UpdateAttendanceAllowanceActualWorkdayRequest(
                seed.SummaryId,
                20.5m,
                seed.OriginalUpdatedAtUtc));

        Assert.Equal(seed.SummaryId, result.Id);
        Assert.Equal(20.5m, result.ActualWorkdayCount);
        var summary = await context.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == seed.SummaryId);
        Assert.Equal(result.ActualAllowanceAmount, summary.AttendanceAllowanceAmount);
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Save_actual_workday_rejects_stale_version_without_partial_aggregate_update()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);
        await using var firstContext = fixture.CreateDbContext();
        var firstScope = new AsyncLocalAuditScope();
        var first = new DatabaseAttendanceAllowanceManualAdjustmentService(
            firstContext, firstScope, new AttendanceAllowanceCalculationPolicy(), new AttendanceAllowanceRequestValidator(), new AttendanceAllowanceWorkdayAdjustmentPolicy());
        await first.UpdateActualWorkdayAsync(new UpdateAttendanceAllowanceActualWorkdayRequest(
            seed.SummaryId, 20m, seed.OriginalUpdatedAtUtc));

        await using var staleContext = fixture.CreateDbContext();
        var stale = new DatabaseAttendanceAllowanceManualAdjustmentService(
            staleContext, new AsyncLocalAuditScope(), new AttendanceAllowanceCalculationPolicy(), new AttendanceAllowanceRequestValidator(), new AttendanceAllowanceWorkdayAdjustmentPolicy());
        var exception = await Assert.ThrowsAsync<AttendanceAllowanceCommandException>(() =>
            stale.UpdateActualWorkdayAsync(new UpdateAttendanceAllowanceActualWorkdayRequest(
                seed.SummaryId, 10m, seed.OriginalUpdatedAtUtc)));

        Assert.Equal(AttendanceAllowanceCommandFailure.Concurrency, exception.Failure);
        await using var verificationContext = fixture.CreateDbContext();
        var detail = await verificationContext.PayrollAttendanceAllowanceRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
        var summary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == seed.SummaryId);
        Assert.Equal(20m, detail.ActualWorkdayCount);
        Assert.Equal(detail.AllowanceAmount, summary.AttendanceAllowanceAmount);
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Save_workdays_updates_both_values_and_aggregate_in_one_versioned_command()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);

        await using var context = fixture.CreateDbContext();
        var service = new DatabaseAttendanceAllowanceManualAdjustmentService(
            context,
            new AsyncLocalAuditScope(),
            new AttendanceAllowanceCalculationPolicy(),
            new AttendanceAllowanceRequestValidator(),
            new AttendanceAllowanceWorkdayAdjustmentPolicy());

        var result = await service.UpdateWorkdaysAsync(new UpdateAttendanceAllowanceWorkdaysRequest(
            seed.SummaryId,
            20.5m,
            27m,
            seed.OriginalUpdatedAtUtc));

        Assert.Equal(20.5m, result.ActualWorkdayCount);
        Assert.Equal(27m, result.StandardWorkdayCount);
        Assert.NotEqual(seed.OriginalUpdatedAtUtc, result.UpdatedAtUtc);

        var detail = await context.PayrollAttendanceAllowanceRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
        var summary = await context.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == seed.SummaryId);
        Assert.Equal(20.5m, detail.ActualWorkdayCount);
        Assert.Equal(27m, detail.StandardWorkdayCount);
        Assert.Equal(detail.AllowanceAmount, summary.AttendanceAllowanceAmount);
        Assert.Equal(detail.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Save_workdays_rejects_a_stale_single_version_without_partial_update()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);

        await using(var firstContext = fixture.CreateDbContext())
        {
            var first = new DatabaseAttendanceAllowanceManualAdjustmentService(
                firstContext,
                new AsyncLocalAuditScope(),
                new AttendanceAllowanceCalculationPolicy(),
                new AttendanceAllowanceRequestValidator(),
                new AttendanceAllowanceWorkdayAdjustmentPolicy());
            await first.UpdateWorkdaysAsync(new UpdateAttendanceAllowanceWorkdaysRequest(
                seed.SummaryId,
                20m,
                27m,
                seed.OriginalUpdatedAtUtc));
        }

        await using var staleContext = fixture.CreateDbContext();
        var stale = new DatabaseAttendanceAllowanceManualAdjustmentService(
            staleContext,
            new AsyncLocalAuditScope(),
            new AttendanceAllowanceCalculationPolicy(),
            new AttendanceAllowanceRequestValidator(),
            new AttendanceAllowanceWorkdayAdjustmentPolicy());

        var exception = await Assert.ThrowsAsync<AttendanceAllowanceCommandException>(() =>
            stale.UpdateWorkdaysAsync(new UpdateAttendanceAllowanceWorkdaysRequest(
                seed.SummaryId,
                10m,
                26m,
                seed.OriginalUpdatedAtUtc)));

        Assert.Equal(AttendanceAllowanceCommandFailure.Concurrency, exception.Failure);
        await using var verificationContext = fixture.CreateDbContext();
        var detail = await verificationContext.PayrollAttendanceAllowanceRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
        var summary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == seed.SummaryId);
        Assert.Equal(20m, detail.ActualWorkdayCount);
        Assert.Equal(27m, detail.StandardWorkdayCount);
        Assert.Equal(detail.AllowanceAmount, summary.AttendanceAllowanceAmount);
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Save_standard_workday_persists_the_adjustment()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);

        await using var context = fixture.CreateDbContext();
        var auditScope = new AsyncLocalAuditScope();
        var service = new DatabaseAttendanceAllowanceManualAdjustmentService(
            context, auditScope, new AttendanceAllowanceCalculationPolicy(), new AttendanceAllowanceRequestValidator(), new AttendanceAllowanceWorkdayAdjustmentPolicy());

        var result = await service.UpdateStandardWorkdayAsync(
            new UpdateAttendanceAllowanceStandardWorkdayRequest(
                seed.SummaryId,
                27m,
                seed.OriginalUpdatedAtUtc));

        Assert.Equal(seed.SummaryId, result.Id);
        Assert.Equal(27m, result.StandardWorkdayCount);
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Refresh_overwrites_workday_adjustments_from_current_source_data()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);

        await using(var seedContext = fixture.CreateDbContext())
        {
            seedContext.BasicSalaryRecords.Add(new BasicSalaryRecordRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = seed.EmployeeId,
                PayrollYear = 2026,
                PayrollMonth = 7,
                BasicSalary = 10_000_000m,
                StandardWorkingDays = 26m,
                DailySalary = 384_615.3846m,
                HourlySalary = 48_076.9231m,
                CreatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateDbContext();
        var auditScope = new AsyncLocalAuditScope();
        var service = new DatabaseAttendanceAllowanceRefreshService(
            context,
            auditScope,
            new AuditedMutation(context, auditScope),
            new DatabaseAttendanceAllowanceWorkdaySource(context),
            new DatabaseBasicSalaryWorkdaySource(context),
            new AttendanceAllowanceWorkdayMetricPolicy(),
            new AttendanceAllowanceCalculationPolicy(),
            new AttendanceAllowanceRequestValidator());

        var result = await service.RefreshAsync(
            new RefreshAttendanceAllowanceRequest(7, 2026, seed.SummaryId));

        var detail = await context.PayrollAttendanceAllowanceRecords
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);

        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(26m, detail.StandardWorkdayCount);
        Assert.Equal(0m, detail.ActualWorkdayCount);
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Refresh_counts_only_status_codes_marked_for_attendance_allowance()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);
        var timestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        await using(var seedContext = fixture.CreateDbContext())
        {
            var attendanceAllowanceStatus = new AttendanceStatusCodeRow
            {
                Id = Guid.NewGuid(),
                Code = "CC",
                Name = "Chuyên cần",
                Kind = "Test",
                PhuCapChuyenCan = true,
                CongHanhChinh = false,
                IsActive = true,
                CreatedAtUtc = timestamp
            };
            var administrativeOnlyStatus = new AttendanceStatusCodeRow
            {
                Id = Guid.NewGuid(),
                Code = "HC",
                Name = "Hành chính",
                Kind = "Test",
                PhuCapChuyenCan = false,
                CongHanhChinh = true,
                IsActive = true,
                CreatedAtUtc = timestamp
            };
            seedContext.BasicSalaryRecords.Add(new BasicSalaryRecordRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = seed.EmployeeId,
                PayrollYear = 2026,
                PayrollMonth = 7,
                BasicSalary = 10_000_000m,
                StandardWorkingDays = 26m,
                DailySalary = 384_615.3846m,
                HourlySalary = 48_076.9231m,
                CreatedAtUtc = timestamp
            });
            seedContext.AttendanceStatusCodes.AddRange(attendanceAllowanceStatus, administrativeOnlyStatus);
            seedContext.AttendanceWorkdaySummaries.AddRange(
                CreateWorkday(seed.EmployeeId, attendanceAllowanceStatus.Id, new DateOnly(2026, 7, 1), timestamp),
                CreateWorkday(seed.EmployeeId, administrativeOnlyStatus.Id, new DateOnly(2026, 7, 2), timestamp),
                CreateWorkday(seed.EmployeeId, attendanceAllowanceStatus.Id, new DateOnly(2026, 7, 3), timestamp, AttendanceWorkCalendarDayTypes.DayOff));
            await seedContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateDbContext();
        var auditScope = new AsyncLocalAuditScope();
        var service = new DatabaseAttendanceAllowanceRefreshService(
            context,
            auditScope,
            new AuditedMutation(context, auditScope),
            new DatabaseAttendanceAllowanceWorkdaySource(context),
            new DatabaseBasicSalaryWorkdaySource(context),
            new AttendanceAllowanceWorkdayMetricPolicy(),
            new AttendanceAllowanceCalculationPolicy(),
            new AttendanceAllowanceRequestValidator());

        await service.RefreshAsync(new RefreshAttendanceAllowanceRequest(7, 2026, seed.SummaryId));

        var detail = await context.PayrollAttendanceAllowanceRecords
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
        Assert.Equal(1m, detail.AdministrativeWorkdayCount);
        Assert.Equal(1m, detail.CtlWorkdayCount.GetValueOrDefault());
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Refresh_skips_a_locked_row_without_changing_its_manual_snapshot()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);
        await using(var lockContext = fixture.CreateDbContext())
        {
            var detail = await lockContext.PayrollAttendanceAllowanceRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
            detail.IsLocked = true;
            await lockContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateDbContext();
        var auditScope = new AsyncLocalAuditScope();
        var service = new DatabaseAttendanceAllowanceRefreshService(
            context, auditScope, new AuditedMutation(context, auditScope), new DatabaseAttendanceAllowanceWorkdaySource(context), new DatabaseBasicSalaryWorkdaySource(context),
            new AttendanceAllowanceWorkdayMetricPolicy(), new AttendanceAllowanceCalculationPolicy(), new AttendanceAllowanceRequestValidator());

        var result = await service.RefreshAsync(new RefreshAttendanceAllowanceRequest(7, 2026, seed.SummaryId));

        Assert.Equal(1, result.MatchedRowCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(12m, (await context.PayrollAttendanceAllowanceRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId)).ActualWorkdayCount);
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Batch_lock_then_unlock_uses_the_version_returned_by_the_previous_command()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);
        await using var context = fixture.CreateDbContext();
        var auditScope = new AsyncLocalAuditScope();
        var requestValidator = new AttendanceAllowanceRequestValidator();
        var service = new DatabaseAttendanceAllowanceLockService(
            context,
            auditScope,
            new AuditedMutation(context, auditScope),
            requestValidator,
            requestValidator);

        var locked = await service.SetLockStateBatchAsync(new SetAttendanceAllowanceBatchLockStateRequest(
            2026, 7, true, AttendanceAllowanceBatchLockScope.SelectedRows,
            Items: [new AttendanceAllowanceLockItem(seed.SummaryId, seed.OriginalUpdatedAtUtc)]));
        var lockedRow = await context.PayrollAttendanceAllowanceRecords.AsNoTracking().SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
        var unlocked = await service.SetLockStateBatchAsync(new SetAttendanceAllowanceBatchLockStateRequest(
            2026, 7, false, AttendanceAllowanceBatchLockScope.SelectedRows,
            Items: [new AttendanceAllowanceLockItem(seed.SummaryId, lockedRow.UpdatedAtUtc)]));

        Assert.Equal(1, locked.TargetRowCount);
        Assert.Equal(1, locked.UpdatedCount);
        Assert.Equal(1, unlocked.UpdatedCount);
        Assert.False((await context.PayrollAttendanceAllowanceRecords.AsNoTracking().SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId)).IsLocked);
    }

    [PostgreSqlAttendanceAllowanceFact]
    public async Task Export_returns_only_the_requested_period_and_escapes_formula_like_employee_data()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);
        await using(var seedContext = fixture.CreateDbContext())
        {
            var employee = await seedContext.Employees.SingleAsync(row => row.Id == seed.EmployeeId);
            employee.EmployeeCode = "=FORMULA";
            employee.FirstName = "+Mai";
            await seedContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateDbContext();
        var auditScope = new AsyncLocalAuditScope();
        var service = new Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries.DatabaseAttendanceAllowanceExportService(
            context, auditScope, new AuditedMutation(context, auditScope), new AttendanceAllowanceRequestValidator());

        var rows = await service.ExportAsync(new AttendanceAllowanceExportRequest(2026, 7, AttendanceAllowanceExportFormat.Pdf));

        var row = Assert.Single(rows);
        Assert.Equal("'=FORMULA", row.EmployeeCode);
        Assert.Equal("'+Mai", row.EmployeeName);
        Assert.Equal("07/2026", row.PayrollPeriodDisplay);
    }

    private static async Task<AttendanceAllowanceSeed> SeedAsync(AttendanceAllowancePostgreSqlFixture fixture)
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-1), DateTimeKind.Unspecified);
        var seedSuffix = Guid.NewGuid().ToString("N")[..8];
        var department = new AttendanceDepartmentRow
        {
            Id = Guid.NewGuid(), Code = $"D-ATT-{seedSuffix}", CenterName = "Test", DepartmentOrWorkshopName = "Payroll", Status = 1, CreatedAtUtc = now
        };
        var position = new AttendanceGatewayPositionRow
        {
            Id = Guid.NewGuid(), Code = $"P-ATT-{seedSuffix}", Name = "Tester", Status = 1, EmployeeCount = 1, CreatedAtUtc = now
        };
        var employee = new AttendanceGatewayEmployeeRow
        {
            Id = Guid.NewGuid(), DepartmentId = department.Id, PositionId = position.Id, EmployeeCode = $"E-ATT-{seedSuffix}", FirstName = "Attendance", LastName = "Tester", HireDate = now, Status = 1, IsDeleted = false, CreatedAtUtc = now
        };
        var summary = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id, PayrollYear = 2026, PayrollMonth = 7, IsLocked = false, CreatedAtUtc = now, CreatedBy = "seed", UpdatedAtUtc = now, UpdatedBy = "seed"
        };
        var detail = new PayrollAttendanceAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summary.Id, StandardAllowanceAmount = 500_000m, StandardWorkdayCount = 26m, ActualWorkdayCount = 12m, AttendanceRate = 1m, AllowanceAmount = 500_000m, AppliedRuleKey = "attendance-ratio", AttendanceClass = "A", CreatedAtUtc = now, CreatedBy = "seed", UpdatedAtUtc = now, UpdatedBy = "seed"
        };

        await using var context = fixture.CreateDbContext();
        context.AddRange(department, position, employee, summary, detail);
        await context.SaveChangesAsync();
        return new AttendanceAllowanceSeed(summary.Id, employee.Id, now);
    }

    private static AttendanceWorkdaySummaryRow CreateWorkday(
        Guid employeeId,
        Guid statusCodeId,
        DateOnly workDate,
        DateTime timestamp,
        string dayType = AttendanceWorkCalendarDayTypes.Regular) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = employeeId,
        WorkDate = workDate,
        DayType = dayType,
        CodeKetQuaTinhCongId = statusCodeId,
        ComputedAtUtc = timestamp,
        CreatedAtUtc = timestamp
    };

    private sealed record AttendanceAllowanceSeed(
        Guid SummaryId,
        Guid EmployeeId,
        DateTime OriginalUpdatedAtUtc);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AttendanceAllowancePostgreSqlCollection
    : ICollectionFixture<AttendanceAllowancePostgreSqlFixture>
{
    public const string Name = "Attendance allowance PostgreSQL integration";
}

public sealed class AttendanceAllowancePostgreSqlFixture : IAsyncLifetime
{
    private const string ConnectionVariable = "VNTA_ATTENDANCE_ALLOWANCE_TEST_DB";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionVariable);
        if(string.IsNullOrWhiteSpace(configured)) return;

        var builder = new NpgsqlConnectionStringBuilder(configured);
        if(string.IsNullOrWhiteSpace(builder.Database) || !builder.Database.StartsWith("vnta_attendance_allowance_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{ConnectionVariable} must target a disposable vnta_attendance_allowance_test* database.");

        connectionString = builder.ConnectionString;
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if(connectionString is null) return;
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
    }

    public void RequireDatabase()
    {
        if(connectionString is null) throw new InvalidOperationException($"Set {ConnectionVariable} to run attendance allowance integration tests.");
    }

    public ApplicationDbContext CreateDbContext()
    {
        RequireDatabase();
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlAttendanceAllowanceFactAttribute : FactAttribute
{
    public PostgreSqlAttendanceAllowanceFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNTA_ATTENDANCE_ALLOWANCE_TEST_DB")))
            Skip = "Set VNTA_ATTENDANCE_ALLOWANCE_TEST_DB to a disposable vnta_attendance_allowance_test* PostgreSQL database to run attendance allowance integration tests.";
    }
}
