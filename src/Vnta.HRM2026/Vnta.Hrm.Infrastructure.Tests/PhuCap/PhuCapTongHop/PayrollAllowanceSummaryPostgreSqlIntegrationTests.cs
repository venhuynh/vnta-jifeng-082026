using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Queries;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Queries;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTongHop;

/// <summary>
/// Opt-in PostgreSQL coverage for provider-specific search and transactional command behavior.
/// The fixture accepts only a disposable database whose name begins with
/// <c>vnta_allowance_summary_test</c>, then recreates and deletes it for the suite.
/// </summary>
[Collection(PayrollAllowanceSummaryPostgreSqlCollection.Name)]
public sealed class PayrollAllowanceSummaryPostgreSqlIntegrationTests(
    PayrollAllowanceSummaryPostgreSqlFixture fixture)
{
    [PostgreSqlPayrollAllowanceSummaryFact]
    public async Task Search_translates_case_insensitive_employee_filter_to_postgresql_ilike()
    {
        fixture.RequireDatabase();
        await SeedAsync(fixture, "NV-SUMMARY-ALPHA");
        await SeedAsync(fixture, "NV-SUMMARY-BETA");

        await using var dbContext = fixture.CreateDbContext();
        var page = await new DatabasePayrollAllowanceSummaryReadService(
            new PayrollAllowanceSummaryPersistence(dbContext, new AsyncLocalAuditScope(), new AuditedMutation(dbContext, new AsyncLocalAuditScope())))
            .SearchAsync(new PayrollAllowanceSummaryFilter(7, 2026, "nv-summary-alpha", Take: 20));

        var row = Assert.Single(page.Rows);
        Assert.Equal("NV-SUMMARY-ALPHA", row.EmployeeCode);
        Assert.Equal(1, page.TotalCount);
    }

    [PostgreSqlPayrollAllowanceSummaryFact]
    public async Task Manual_note_from_a_stale_db_context_is_rejected_and_preserves_the_first_commit()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture, "NV-SUMMARY-CONCURRENCY");

        await using var staleContext = fixture.CreateDbContext();
        await using var freshContext = fixture.CreateDbContext();
        var staleService = CreateManualService(staleContext);
        var freshService = CreateManualService(freshContext);

        await freshService.UpdateManualValuesAsync(
            new UpdatePayrollAllowanceSummaryManualNoteRequest(seed.SummaryId, "first commit", seed.Version, "first-actor"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            staleService.UpdateManualValuesAsync(
                new UpdatePayrollAllowanceSummaryManualNoteRequest(seed.SummaryId, "stale overwrite", seed.Version, "second-actor")));

        await using var verificationContext = fixture.CreateDbContext();
        var persisted = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == seed.SummaryId);
        Assert.Equal("first commit", persisted.Note);
        Assert.Equal("first-actor", persisted.UpdatedBy);
    }

    [PostgreSqlPayrollAllowanceSummaryFact]
    public async Task Batch_lock_rolls_back_the_summary_change_when_the_operation_audit_insert_fails()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture, "NV-SUMMARY-AUDIT");
        const string failedCorrelationId = "allowance-summary-audit-failure";
        var auditScope = new AsyncLocalAuditScope();

        await using var dbContext = fixture.CreateDbContext();
        await CreateAuditWriteFailureTriggerAsync(dbContext);
        try
        {
            var persistence = new PayrollAllowanceSummaryPersistence(
                dbContext,
                auditScope,
                new AuditedMutation(dbContext, auditScope));
            var lockService = new DatabasePayrollAllowanceSummaryLockService(persistence);

            using(auditScope.Begin(CreateAuditCommand(failedCorrelationId)))
            {
                await Assert.ThrowsAsync<DbUpdateException>(() => lockService.SetLockStateBatchAsync(
                    new SetPayrollAllowanceSummaryBatchLockStateRequest(
                        2026,
                        7,
                        true,
                        [seed.SummaryId],
                        [new PayrollAllowanceSummaryLockStateConcurrencyToken(seed.SummaryId, seed.Version)],
                        "payroll-admin")));
            }
        }
        finally
        {
            await DropAuditWriteFailureTriggerAsync(dbContext);
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.False(await verificationContext.PayrollAllowanceSummaryRecords
            .Where(row => row.Id == seed.SummaryId)
            .Select(row => row.IsLocked)
            .SingleAsync());
        Assert.False(await verificationContext.AuditEvents.AnyAsync(row => row.CorrelationId == failedCorrelationId));
    }

    private static DatabasePayrollAllowanceSummaryManualAdjustmentService CreateManualService(ApplicationDbContext dbContext) =>
        new(new PayrollAllowanceSummaryPersistence(dbContext, new AsyncLocalAuditScope(), new AuditedMutation(dbContext, new AsyncLocalAuditScope())));

    private static async Task<PayrollAllowanceSummarySeed> SeedAsync(
        PayrollAllowanceSummaryPostgreSqlFixture fixture,
        string employeeCode)
    {
        var now = new DateTime(2026, 7, 1, 8, 0, 0);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var department = new AttendanceDepartmentRow
        {
            Id = Guid.NewGuid(), Code = $"D-SUM-{suffix}", CenterName = "Test",
            DepartmentOrWorkshopName = "Allowance summary", Status = 1, CreatedAtUtc = now
        };
        var position = new AttendanceGatewayPositionRow
        {
            Id = Guid.NewGuid(), Code = $"P-SUM-{suffix}", Name = "Allowance summary tester",
            Status = 1, EmployeeCount = 1, CreatedAtUtc = now
        };
        var employee = new AttendanceGatewayEmployeeRow
        {
            Id = Guid.NewGuid(), EmployeeCode = employeeCode, FirstName = "Summary", LastName = "Test",
            DepartmentId = department.Id, PositionId = position.Id, Status = 1, IsDeleted = false,
            HireDate = now, CreatedAtUtc = now
        };
        var summary = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id, PayrollMonth = 7, PayrollYear = 2026,
            ResponsibilityAllowanceAmount = 1m, ResponsibilityOtherAllowanceAmount = 2m,
            SeniorityAllowanceAmount = 3m, AttendanceAllowanceAmount = 4m, MealAllowanceAmount = 5m,
            HazardAllowanceAmount = 6m, OtherAllowanceAmount = 7m, LeaveHolidayAllowanceAmount = 8m,
            IsLocked = false, CreatedAtUtc = now, CreatedBy = "seed", UpdatedAtUtc = now, UpdatedBy = "seed"
        };

        await using var dbContext = fixture.CreateDbContext();
        dbContext.AddRange(department, position, employee, summary);
        await dbContext.SaveChangesAsync();
        return new PayrollAllowanceSummarySeed(summary.Id, now);
    }

    private static Task CreateAuditWriteFailureTriggerAsync(ApplicationDbContext dbContext) =>
        dbContext.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION audit.reject_allowance_summary_audit_insert()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF NEW.correlation_id = 'allowance-summary-audit-failure' THEN
                    RAISE EXCEPTION 'Intentional allowance-summary audit failure.' USING ERRCODE = 'P0001';
                END IF;
                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER trg_reject_allowance_summary_audit_insert
            BEFORE INSERT ON audit.events
            FOR EACH ROW EXECUTE FUNCTION audit.reject_allowance_summary_audit_insert();
            """);

    private static Task DropAuditWriteFailureTriggerAsync(ApplicationDbContext dbContext) =>
        dbContext.Database.ExecuteSqlRawAsync("""
            DROP TRIGGER IF EXISTS trg_reject_allowance_summary_audit_insert ON audit.events;
            DROP FUNCTION IF EXISTS audit.reject_allowance_summary_audit_insert();
            """);

    private static AuditCommand CreateAuditCommand(string correlationId) => new(
        Guid.NewGuid(),
        AuditActions.AllowanceSummary.BatchLockStateChanged,
        new AuditActor("postgres-test", "PostgreSQL test", AuditActorKind.Service, AuditSource.Worker),
        correlationId,
        AuditCaptureMode.OperationOnly);

    private sealed record PayrollAllowanceSummarySeed(Guid SummaryId, DateTime Version);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PayrollAllowanceSummaryPostgreSqlCollection
    : ICollectionFixture<PayrollAllowanceSummaryPostgreSqlFixture>
{
    public const string Name = "Payroll allowance summary PostgreSQL integration";
}

public sealed class PayrollAllowanceSummaryPostgreSqlFixture : IAsyncLifetime
{
    public const string ConnectionVariable = "VNTA_ALLOWANCE_SUMMARY_TEST_DB";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionVariable);
        if(string.IsNullOrWhiteSpace(configured))
            return;

        var builder = new NpgsqlConnectionStringBuilder(configured);
        if(string.IsNullOrWhiteSpace(builder.Database)
            || !builder.Database.StartsWith("vnta_allowance_summary_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionVariable} must target a disposable vnta_allowance_summary_test* database.");
        }

        connectionString = builder.ConnectionString;
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if(connectionString is null)
            return;

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    public void RequireDatabase()
    {
        if(connectionString is null)
            throw new InvalidOperationException(
                $"Set {ConnectionVariable} to run payroll allowance-summary PostgreSQL integration tests.");
    }

    public ApplicationDbContext CreateDbContext()
    {
        RequireDatabase();
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlPayrollAllowanceSummaryFactAttribute : FactAttribute
{
    public PostgreSqlPayrollAllowanceSummaryFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PayrollAllowanceSummaryPostgreSqlFixture.ConnectionVariable)))
        {
            Skip = $"Set {PayrollAllowanceSummaryPostgreSqlFixture.ConnectionVariable} to a disposable vnta_allowance_summary_test* PostgreSQL database to run this test.";
        }
    }
}
