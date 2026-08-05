using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapDocHai;

/// <summary>
/// Regression suite opt-in cho concurrency của Phụ cấp độc hại. Database phải là disposable;
/// test không chạy với connection string production hoặc development.
/// </summary>
[Collection(HazardAllowancePostgreSqlCollection.Name)]
public sealed class HazardAllowancePostgreSqlIntegrationTests(HazardAllowancePostgreSqlFixture fixture)
{
    [PostgreSqlHazardAllowanceFact]
    public async Task Summary_query_translates_and_counts_hazard_snapshot_buckets()
    {
        fixture.RequireDatabase();
        await SeedAsync(fixture, payrollMonth: 8);

        await using var context = fixture.CreateDbContext();
        var summary = await CreateReadService(context).GetSummaryAsync(
            new HazardAllowanceFilter(8, 2026, HazardAllowanceLockState.All, null));

        Assert.Equal(1, summary.TotalCount);
        Assert.Equal(1, summary.EligibleCount);
        Assert.Equal(0, summary.ExceptionCount);
        Assert.Equal(0, summary.LockedCount);
        Assert.Equal(1, summary.OpenCount);
    }

    [PostgreSqlHazardAllowanceFact]
    public async Task View_action_loads_summary_and_first_page_for_the_selected_period()
    {
        fixture.RequireDatabase();
        await SeedAsync(fixture, payrollMonth: 9);

        await using var context = fixture.CreateDbContext();
        var service = CreateReadService(context);
        var filter = new HazardAllowanceFilter(9, 2026, HazardAllowanceLockState.All, null, Take: 20);

        var summary = await service.GetSummaryAsync(filter);
        var page = await service.SearchPageAsync(filter);

        Assert.Equal(1, summary.TotalCount);
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Rows);
    }

    [PostgreSqlHazardAllowanceFact]
    public async Task Batch_lock_scopes_targets_to_the_requested_hazard_period()
    {
        fixture.RequireDatabase();
        var targetPeriodSeed = await SeedAsync(fixture, payrollMonth: 10);
        var otherPeriodSeed = await SeedAsync(fixture, payrollMonth: 11);

        await using var context = fixture.CreateDbContext();
        var service = new DatabaseHazardAllowanceLockService(context, new HazardAllowanceLockStatePolicy(), new HazardAllowanceRequestValidator());

        var wholePeriodResult = await service.SetLockStateBatchAsync(
            new SetHazardAllowanceBatchLockStateRequest(2026, 10, true, null, "payroll-admin"));
        var outsidePeriodResult = await service.SetLockStateBatchAsync(
            new SetHazardAllowanceBatchLockStateRequest(
                2026,
                10,
                true,
                [otherPeriodSeed.SummaryId],
                "payroll-admin"));

        Assert.Equal(1, wholePeriodResult.TargetRowCount);
        Assert.Equal(1, wholePeriodResult.UpdatedCount);
        Assert.Equal(0, outsidePeriodResult.TargetRowCount);
        Assert.Equal(0, outsidePeriodResult.UpdatedCount);
        Assert.True(await context.PayrollAllowanceSummaryRecords
            .Where(row => row.Id == targetPeriodSeed.SummaryId)
            .Select(row => row.IsLocked)
            .SingleAsync());
        Assert.False(await context.PayrollAllowanceSummaryRecords
            .Where(row => row.Id == otherPeriodSeed.SummaryId)
            .Select(row => row.IsLocked)
            .SingleAsync());
    }

    [PostgreSqlHazardAllowanceFact]
    public async Task Manual_update_with_stale_snapshot_returns_domain_conflict()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture);

        await using (var firstContext = fixture.CreateDbContext())
        {
            var detail = await firstContext.PayrollHazardAllowanceRecords
                .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
            var summary = await firstContext.PayrollAllowanceSummaryRecords
                .SingleAsync(row => row.Id == seed.SummaryId);
            await CreateManualAdjustmentService(firstContext).UpdateManualValuesAsync(
                CreateRequest(detail, summary, 15_400m, "first-actor"));
        }

        await using var staleContext = fixture.CreateDbContext();
        var staleDetail = await staleContext.PayrollHazardAllowanceRecords
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
        var staleSummary = await staleContext.PayrollAllowanceSummaryRecords
            .SingleAsync(row => row.Id == seed.SummaryId);
        var staleRequest = new UpdateHazardAllowanceManualValuesRequest(
            staleDetail.PayrollAllowanceSummaryRecordId,
            2m,
            0m,
            7700m,
            23_100m,
            true,
            null,
            seed.OriginalDetailUpdatedAtUtc,
            seed.OriginalSummaryUpdatedAtUtc,
            "second-actor");

        await Assert.ThrowsAsync<HazardAllowanceConflictException>(() =>
            CreateManualAdjustmentService(staleContext).UpdateManualValuesAsync(staleRequest));
    }

    private static async Task<HazardSeed> SeedAsync(HazardAllowancePostgreSqlFixture fixture, short payrollMonth = 7)
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var seedSuffix = Guid.NewGuid().ToString("N")[..8];
        var department = new AttendanceDepartmentRow { Id = Guid.NewGuid(), Code = $"D-HAZ-{seedSuffix}", CenterName = "Test", DepartmentOrWorkshopName = "Production", Status = 1, CreatedAtUtc = now };
        var position = new AttendanceGatewayPositionRow { Id = Guid.NewGuid(), Code = $"P-HAZ-{seedSuffix}", Name = "Worker", Status = 1, EmployeeCount = 1, CreatedAtUtc = now };
        var employee = new AttendanceGatewayEmployeeRow { Id = Guid.NewGuid(), DepartmentId = department.Id, PositionId = position.Id, EmployeeCode = $"E-HAZ-{seedSuffix}", FirstName = "Hazard", LastName = "Test", HireDate = now, Status = 1, IsDeleted = false, CreatedAtUtc = now };
        var summary = new PayrollAllowanceSummaryRecordRow { Id = Guid.NewGuid(), EmployeeId = employee.Id, PayrollYear = 2026, PayrollMonth = payrollMonth, IsLocked = false, CreatedAtUtc = now, CreatedBy = "seed" };
        var detail = new PayrollHazardAllowanceRecordRow { PayrollAllowanceSummaryRecordId = summary.Id, QualifiedWorkdayCount = 2m, LateEarlyDeductionDays = 0m, PayableWorkdayCount = 2m, HazardAllowancePerDay = 7700m, HazardAllowanceAmount = 15_400m, IsEligibleDepartment = true, IsEligibleForAllowance = true, CreatedAtUtc = now, CreatedBy = "seed", UpdatedAtUtc = now, UpdatedBy = "seed" };

        await using var context = fixture.CreateDbContext();
        context.AddRange(department, position, employee, summary, detail);
        await context.SaveChangesAsync();
        return new HazardSeed(summary.Id, now, now);
    }

    private static UpdateHazardAllowanceManualValuesRequest CreateRequest(PayrollHazardAllowanceRecordRow detail, PayrollAllowanceSummaryRecordRow summary, decimal amount, string actor) =>
        new(detail.PayrollAllowanceSummaryRecordId, 2m, 0m, 7700m, amount, true, null, detail.UpdatedAtUtc ?? detail.CreatedAtUtc, summary.UpdatedAtUtc ?? summary.CreatedAtUtc, actor);

    private static DatabaseHazardAllowanceReadService CreateReadService(ApplicationDbContext context) =>
        new(new HazardAllowanceReadProjection(context, new HazardAllowanceRequestValidator()));

    private static DatabaseHazardAllowanceManualAdjustmentService CreateManualAdjustmentService(ApplicationDbContext context) =>
        new(context, new HazardAllowanceManualAdjustmentPolicy(), new HazardAllowanceRequestValidator());

    private sealed record HazardSeed(
        Guid SummaryId,
        DateTime OriginalDetailUpdatedAtUtc,
        DateTime OriginalSummaryUpdatedAtUtc);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HazardAllowancePostgreSqlCollection : ICollectionFixture<HazardAllowancePostgreSqlFixture>
{
    public const string Name = "Hazard allowance PostgreSQL integration";
}

public sealed class HazardAllowancePostgreSqlFixture : IAsyncLifetime
{
    private const string ConnectionVariable = "VNTA_HAZARD_ALLOWANCE_TEST_DB";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionVariable);
        if(string.IsNullOrWhiteSpace(configured)) return;
        var builder = new NpgsqlConnectionStringBuilder(configured);
        if(string.IsNullOrWhiteSpace(builder.Database) || !builder.Database.StartsWith("vnta_hazard_allowance_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{ConnectionVariable} must target a disposable vnta_hazard_allowance_test* database.");
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
        if(connectionString is null) throw new InvalidOperationException($"Set {ConnectionVariable} to run hazard allowance PostgreSQL integration tests.");
    }

    public ApplicationDbContext CreateDbContext()
    {
        RequireDatabase();
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlHazardAllowanceFactAttribute : FactAttribute
{
    public PostgreSqlHazardAllowanceFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNTA_HAZARD_ALLOWANCE_TEST_DB")))
            Skip = "Set VNTA_HAZARD_ALLOWANCE_TEST_DB to a disposable vnta_hazard_allowance_test* PostgreSQL database to run hazard integration tests.";
    }
}
