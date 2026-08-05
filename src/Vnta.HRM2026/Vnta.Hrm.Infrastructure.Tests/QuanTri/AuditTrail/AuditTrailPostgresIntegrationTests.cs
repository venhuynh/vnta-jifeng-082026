using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.CaKip.CaiDatCa;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.QuanTri.AuditTrail;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AuditTrailPostgresCollection : ICollectionFixture<AuditTrailPostgresFixture>
{
    public const string Name = "Audit trail PostgreSQL integration";
}

/// <summary>
/// Runs against a disposable database named vnta_audit_test*. CI supplies this database through
/// VNTA_AUDIT_TEST_DB. Local test runs skip this suite unless the explicit test connection exists.
/// </summary>
[Collection(AuditTrailPostgresCollection.Name)]
public sealed class AuditTrailPostgresIntegrationTests
{
    private readonly AuditTrailPostgresFixture _fixture;

    public AuditTrailPostgresIntegrationTests(AuditTrailPostgresFixture fixture) =>
        _fixture = fixture;

    [PostgreSqlAuditFact]
    public async Task Tracked_write_commits_the_entity_and_its_audit_diff_together()
    {
        _fixture.RequireDatabase();
        const string correlationId = "postgres-commit";
        var scope = new AsyncLocalAuditScope();

        await using (var dbContext = _fixture.CreateDbContext(scope))
        {
            using var auditScope = scope.Begin(CreateCommand(correlationId));
            dbContext.Shifts.Add(CreateShift("Committed shift"));

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext = _fixture.CreateVerificationDbContext();
        var auditEvent = await verificationContext.AuditEvents
            .Include(x => x.PropertyChanges)
            .SingleAsync(x => x.CorrelationId == correlationId);

        Assert.Equal(AuditEntityTypes.Shift, auditEvent.EntityType);
        Assert.Equal(AuditActions.Shift.Save, auditEvent.Action);
        Assert.Contains(
            auditEvent.PropertyChanges,
            change => change.PropertyName == nameof(AttendanceShiftRow.Name)
                && change.NewDisplay == "Committed shift");
    }

    [PostgreSqlAuditFact]
    public async Task Transaction_rollback_leaves_no_orphan_audit_event()
    {
        _fixture.RequireDatabase();
        const string correlationId = "postgres-rollback";
        var shift = CreateShift("Rolled back shift");
        var scope = new AsyncLocalAuditScope();

        await using (var dbContext = _fixture.CreateDbContext(scope))
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            using var auditScope = scope.Begin(CreateCommand(correlationId));
            dbContext.Shifts.Add(shift);

            await dbContext.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verificationContext = _fixture.CreateVerificationDbContext();
        Assert.False(await verificationContext.Shifts.AnyAsync(x => x.Id == shift.Id));
        Assert.False(await verificationContext.AuditEvents.AnyAsync(x => x.CorrelationId == correlationId));
    }

    [PostgreSqlAuditFact]
    public async Task Concurrency_failure_removes_the_pending_audit_capture_before_retry()
    {
        _fixture.RequireDatabase();
        var shift = CreateShift("Original shift");

        await using (var seedContext = _fixture.CreateVerificationDbContext())
        {
            seedContext.Shifts.Add(shift);
            await seedContext.SaveChangesAsync();
        }

        const string successfulCorrelationId = "postgres-concurrency-success";
        const string failedCorrelationId = "postgres-concurrency-failed";
        var firstScope = new AsyncLocalAuditScope();
        var secondScope = new AsyncLocalAuditScope();

        await using var firstContext = _fixture.CreateDbContext(firstScope);
        await using var secondContext = _fixture.CreateDbContext(secondScope);
        var first = await firstContext.Shifts.SingleAsync(x => x.Id == shift.Id);
        var second = await secondContext.Shifts.SingleAsync(x => x.Id == shift.Id);

        using (firstScope.Begin(CreateCommand(successfulCorrelationId)))
        {
            first.Name = "First update";
            await firstContext.SaveChangesAsync();
        }

        using (secondScope.Begin(CreateCommand(failedCorrelationId)))
        {
            second.Name = "Stale update";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
        }

        await using var verificationContext = _fixture.CreateVerificationDbContext();
        Assert.True(await verificationContext.AuditEvents.AnyAsync(x => x.CorrelationId == successfulCorrelationId));
        Assert.False(await verificationContext.AuditEvents.AnyAsync(x => x.CorrelationId == failedCorrelationId));
        Assert.DoesNotContain(
            secondContext.ChangeTracker.Entries<AuditEventRow>(),
            entry => entry.State != EntityState.Detached);
    }

    [PostgreSqlAuditFact]
    public async Task Append_only_trigger_rejects_direct_audit_mutation()
    {
        _fixture.RequireDatabase();
        const string correlationId = "postgres-append-only";
        var scope = new AsyncLocalAuditScope();

        await using (var dbContext = _fixture.CreateDbContext(scope))
        {
            using var auditScope = scope.Begin(CreateCommand(correlationId));
            dbContext.Shifts.Add(CreateShift("Append-only shift"));
            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext = _fixture.CreateVerificationDbContext();
        var eventId = await verificationContext.AuditEvents
            .Where(x => x.CorrelationId == correlationId)
            .Select(x => x.Id)
            .SingleAsync();
        const string tamperedAction = "Tampered";

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            verificationContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE audit.events SET action = {tamperedAction} WHERE id = {eventId}"));

        Assert.Equal("55000", exception.SqlState);
    }

    [PostgreSqlAuditFact]
    public async Task Audit_write_failure_rolls_back_the_business_mutation_and_allows_a_clean_retry()
    {
        _fixture.RequireDatabase();
        const string failedCorrelationId = "postgres-audit-write-failure";
        const string retryCorrelationId = "postgres-audit-write-retry";
        var shift = CreateShift("Audit write retry shift");
        var scope = new AsyncLocalAuditScope();

        await using var dbContext = _fixture.CreateDbContext(scope);
        await CreateAuditWriteFailureTriggerAsync(dbContext);

        try
        {
            using (scope.Begin(CreateCommand(failedCorrelationId)))
            {
                dbContext.Shifts.Add(shift);
                await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
            }

            Assert.DoesNotContain(
                dbContext.ChangeTracker.Entries<AuditEventRow>(),
                entry => entry.State != EntityState.Detached);

            await DropAuditWriteFailureTriggerAsync(dbContext);

            using (scope.Begin(CreateCommand(retryCorrelationId)))
            {
                await dbContext.SaveChangesAsync();
            }
        }
        finally
        {
            await DropAuditWriteFailureTriggerAsync(dbContext);
        }

        await using var verificationContext = _fixture.CreateVerificationDbContext();
        Assert.True(await verificationContext.Shifts.AnyAsync(x => x.Id == shift.Id));
        Assert.False(await verificationContext.AuditEvents.AnyAsync(x => x.CorrelationId == failedCorrelationId));
        Assert.True(await verificationContext.AuditEvents.AnyAsync(x => x.CorrelationId == retryCorrelationId));
    }

    [PostgreSqlAuditFact]
    public async Task Operation_mutation_commits_one_operation_event_without_row_level_diffs()
    {
        _fixture.RequireDatabase();
        const string correlationId = "postgres-operation-commit";
        var scope = new AsyncLocalAuditScope();
        var shift = CreateShift("Operation-only shift");

        await using (var dbContext = _fixture.CreateDbContext(scope))
        {
            var mutation = new AuditedMutation(dbContext, scope);
            var command = CreateCommand(correlationId) with
            {
                ActionIntent = AuditActions.ShiftAssignment.BatchGenerate,
                CaptureMode = AuditCaptureMode.OperationOnly
            };

            await mutation.ExecuteAsync(
                command,
                _ =>
                {
                    dbContext.Shifts.Add(shift);
                    return Task.FromResult(1);
                },
                affectedCount => new AuditOperationEvent(
                    AuditActions.ShiftAssignment.BatchGenerated,
                    AuditEntityTypes.ShiftAssignment,
                    EntityId: shift.Id.ToString("D"),
                    EntityDisplayName: shift.Name,
                    Outcome: AuditOperationOutcome.Succeeded,
                    Metadata: new Dictionary<string, string>
                    {
                        ["affectedCount"] = affectedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["ruleVersion"] = "test"
                    }));
        }

        await using var verificationContext = _fixture.CreateVerificationDbContext();
        var auditEvent = await verificationContext.AuditEvents
            .Include(x => x.PropertyChanges)
            .SingleAsync(x => x.CorrelationId == correlationId);

        Assert.True(await verificationContext.Shifts.AnyAsync(x => x.Id == shift.Id));
        Assert.Equal(AuditActions.ShiftAssignment.BatchGenerated, auditEvent.Action);
        Assert.Empty(auditEvent.PropertyChanges);
    }

    [PostgreSqlAuditFact]
    public async Task Operation_mutation_failure_rolls_back_the_business_mutation()
    {
        _fixture.RequireDatabase();
        const string correlationId = "postgres-operation-failure";
        var scope = new AsyncLocalAuditScope();
        var shift = CreateShift("Failed operation shift");

        await using (var dbContext = _fixture.CreateDbContext(scope))
        {
            var mutation = new AuditedMutation(dbContext, scope);
            var command = CreateCommand(correlationId) with
            {
                ActionIntent = AuditActions.ShiftAssignment.BatchGenerate,
                CaptureMode = AuditCaptureMode.OperationOnly
            };

            await Assert.ThrowsAsync<ArgumentException>(() => mutation.ExecuteAsync(
                command,
                _ =>
                {
                    dbContext.Shifts.Add(shift);
                    return Task.FromResult(1);
                },
                _ => new AuditOperationEvent(
                    new string('a', 101),
                    AuditEntityTypes.ShiftAssignment,
                    Outcome: AuditOperationOutcome.Succeeded)));
        }

        await using var verificationContext = _fixture.CreateVerificationDbContext();
        Assert.False(await verificationContext.Shifts.AnyAsync(x => x.Id == shift.Id));
        Assert.False(await verificationContext.AuditEvents.AnyAsync(x => x.CorrelationId == correlationId));
    }

    private static Task CreateAuditWriteFailureTriggerAsync(ApplicationDbContext dbContext) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE FUNCTION audit.reject_test_insert()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF NEW.correlation_id = 'postgres-audit-write-failure' THEN
                    RAISE EXCEPTION 'Intentional audit write failure.' USING ERRCODE = 'P0001';
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER trg_test_reject_audit_insert
            BEFORE INSERT ON audit.events
            FOR EACH ROW EXECUTE FUNCTION audit.reject_test_insert();
            """);

    private static Task DropAuditWriteFailureTriggerAsync(ApplicationDbContext dbContext) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """
            DROP TRIGGER IF EXISTS trg_test_reject_audit_insert ON audit.events;
            DROP FUNCTION IF EXISTS audit.reject_test_insert();
            """);

    private static AuditCommand CreateCommand(string correlationId) =>
        new(
            Guid.NewGuid(),
            AuditActions.Shift.Save,
            new AuditActor("postgres-test", "PostgreSQL test", AuditActorKind.Service, AuditSource.Worker),
            correlationId);

    private static AttendanceShiftRow CreateShift(string name) => new()
    {
        Id = Guid.NewGuid(),
        Code = $"AUD-{Guid.NewGuid():N}"[..20],
        Name = name,
        DepartmentGroup = "Audit tests",
        StartTime = "08:00",
        EndTime = "17:00",
        IsOvernight = false,
        Status = 1,
        CreatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
        WorkingDays = "1,2,3,4,5"
    };
}

public sealed class AuditTrailPostgresFixture : IAsyncLifetime
{
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable("VNTA_AUDIT_TEST_DB");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(configuredConnectionString);
        var databaseName = connectionStringBuilder.Database;
        if (string.IsNullOrWhiteSpace(databaseName)
            || !databaseName.StartsWith("vnta_audit_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "VNTA_AUDIT_TEST_DB must target a disposable database whose name starts with 'vnta_audit_test'.");
        }

        _connectionString = connectionStringBuilder.ConnectionString;

        await using var dbContext = CreateVerificationDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await CreateAppendOnlyTriggersAsync(dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        await using var dbContext = CreateVerificationDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    public void RequireDatabase()
    {
        if (_connectionString is null)
        {
            throw new InvalidOperationException(
                "VNTA_AUDIT_TEST_DB is required for PostgreSQL audit integration tests.");
        }
    }

    public ApplicationDbContext CreateDbContext(IAuditScope scope)
    {
        RequireDatabase();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(new AuditSaveChangesInterceptor(scope, new AuditPolicy()))
            .Options;

        return new ApplicationDbContext(options);
    }

    public ApplicationDbContext CreateVerificationDbContext()
    {
        RequireDatabase();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task CreateAppendOnlyTriggersAsync(ApplicationDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE FUNCTION audit.reject_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'Audit records are append-only.' USING ERRCODE = '55000';
            END;
            $$;

            CREATE TRIGGER trg_events_append_only
            BEFORE UPDATE OR DELETE ON audit.events
            FOR EACH ROW EXECUTE FUNCTION audit.reject_mutation();

            CREATE TRIGGER trg_property_changes_append_only
            BEFORE UPDATE OR DELETE ON audit.property_changes
            FOR EACH ROW EXECUTE FUNCTION audit.reject_mutation();
            """);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlAuditFactAttribute : FactAttribute
{
    public PostgreSqlAuditFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNTA_AUDIT_TEST_DB")))
        {
            Skip = "Set VNTA_AUDIT_TEST_DB to a disposable vnta_audit_test* PostgreSQL database to run audit integration tests.";
        }
    }
}
