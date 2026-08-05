using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Queries;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapCom;

/// <summary>
/// Opt-in PostgreSQL characterization. The fixture accepts only an explicitly named disposable database,
/// deletes it before and after the suite, and therefore can never target development or production data.
/// </summary>
[Collection(MealAllowancePostgreSqlCollection.Name)]
public sealed class MealAllowancePostgreSqlIntegrationTests(MealAllowancePostgreSqlFixture fixture)
{
    [PostgreSqlMealAllowanceFact]
    public async Task Search_page_translates_case_insensitive_employee_filter_to_postgresql_ilike()
    {
        fixture.RequireDatabase();
        await SeedAsync(fixture, "NV-MEAL-ALPHA");
        await SeedAsync(fixture, "NV-MEAL-BETA");

        await using var dbContext = fixture.CreateDbContext();
        var page = await new DatabaseMealAllowanceReadService(dbContext, new MealAllowanceRequestValidator()).SearchPageAsync(
            new MealAllowanceFilter(7, 2026, "nV-mEaL-aLpHa", Take: 20));

        var row = Assert.Single(page.Rows);
        Assert.Equal("NV-MEAL-ALPHA", row.EmployeeCode);
        Assert.Equal(1, page.TotalCount);
    }

    [PostgreSqlMealAllowanceFact]
    public async Task Manual_adjustment_from_a_stale_context_returns_conflict_and_preserves_the_first_commit()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture, "NV-MEAL-CONCURRENCY", updatedAtUtc: new DateTime(2026, 7, 1, 8, 0, 0));

        await using var staleContext = fixture.CreateDbContext();
        var staleDetail = await staleContext.PayrollMealAllowanceRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
        await using (var freshContext = fixture.CreateDbContext())
        {
            await new DatabaseMealAllowanceManualAdjustmentService(freshContext, new MealAllowanceRequestValidator()).UpdateManualValuesAsync(
                new UpdateMealAllowanceManualValuesRequest(seed.SummaryId, 2, "first", seed.UpdatedAtUtc, "first-actor"));
        }

        await Assert.ThrowsAsync<MealAllowanceConflictException>(() =>
            new DatabaseMealAllowanceManualAdjustmentService(staleContext, new MealAllowanceRequestValidator()).UpdateManualValuesAsync(
                new UpdateMealAllowanceManualValuesRequest(seed.SummaryId, 3, "stale", staleDetail.UpdatedAtUtc, "second-actor")));

        await using var verificationContext = fixture.CreateDbContext();
        var persisted = await verificationContext.PayrollMealAllowanceRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId);
        Assert.Equal(2, persisted.QualifiedMealDays);
        Assert.Equal("first", persisted.Note);
    }

    [PostgreSqlMealAllowanceFact]
    public async Task Failed_save_rolls_back_lock_state_when_another_pending_change_violates_a_database_constraint()
    {
        fixture.RequireDatabase();
        var seed = await SeedAsync(fixture, "NV-MEAL-TRANSACTION");
        await using var dbContext = fixture.CreateDbContext();
        var sourceEmployee = await dbContext.Employees.SingleAsync(employee => employee.Id == seed.EmployeeId);
        var pendingNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var invalidEmployeeId = Guid.NewGuid();
        var invalidSummaryId = Guid.NewGuid();
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = invalidEmployeeId,
            EmployeeCode = "NV-MEAL-INVALID",
            FirstName = "Invalid",
            LastName = "Pending",
            Status = 1,
            DepartmentId = sourceEmployee.DepartmentId,
            PositionId = sourceEmployee.PositionId,
            HireDate = pendingNow,
            CreatedAtUtc = pendingNow
        });
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = invalidSummaryId,
            EmployeeId = invalidEmployeeId,
            PayrollMonth = 7,
            PayrollYear = 2026,
            CreatedAtUtc = pendingNow,
            CreatedBy = "test"
        });
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = invalidSummaryId,
            QualifiedMealDays = -1,
            Overtime1900Days = 0,
            MealAllowancePerQualifiedDay = 18_000m,
            MealAllowanceAmount = 0m,
            RuleCode = MealAllowancePolicy.QualifiedMealRuleCode,
            CalculatedAtUtc = pendingNow,
            CreatedAtUtc = pendingNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => new DatabaseMealAllowanceLockService(dbContext, new MealAllowanceRequestValidator())
            .SetLockStateBatchAsync(new SetMealAllowanceLockStateBatchRequest(
                2026, 7, true, MealAllowanceLockActionScope.SelectedRows, [seed.SummaryId], "payroll-admin")));

        await using var verificationContext = fixture.CreateDbContext();
        Assert.False(await verificationContext.PayrollMealAllowanceRecords
            .Where(row => row.PayrollAllowanceSummaryRecordId == seed.SummaryId)
            .Select(row => row.IsLocked)
            .SingleAsync());
    }

    private static async Task<MealAllowanceSeed> SeedAsync(
        MealAllowancePostgreSqlFixture fixture,
        string employeeCode,
        DateTime? updatedAtUtc = null)
    {
        var now = updatedAtUtc ?? new DateTime(2026, 7, 1, 8, 0, 0);
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Departments.Add(new AttendanceDepartmentRow
        {
            Id = departmentId,
            Code = $"D-{employeeCode}",
            CenterName = "Test",
            DepartmentOrWorkshopName = "Meal allowance test",
            Status = 1,
            CreatedAtUtc = now
        });
        dbContext.Positions.Add(new AttendanceGatewayPositionRow
        {
            Id = positionId,
            Code = $"P-{employeeCode}",
            Name = "Meal allowance test",
            Status = 1,
            EmployeeCount = 1,
            CreatedAtUtc = now
        });
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = employeeId,
            EmployeeCode = employeeCode,
            FirstName = "Meal",
            LastName = "Test",
            Status = 1,
            DepartmentId = departmentId,
            PositionId = positionId,
            HireDate = now,
            CreatedAtUtc = now,
            IsDeleted = false
        });
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = employeeId,
            PayrollMonth = 7,
            PayrollYear = 2026,
            MealAllowanceAmount = 18_000m,
            CreatedAtUtc = now,
            CreatedBy = "seed"
        });
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            QualifiedMealDays = 1,
            Overtime1900Days = 1,
            MealAllowancePerQualifiedDay = 18_000m,
            MealAllowanceAmount = 18_000m,
            RuleCode = MealAllowancePolicy.QualifiedMealRuleCode,
            RuleVersion = MealAllowancePolicy.QualifiedMealRuleVersion,
            CalculatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            UpdatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();
        return new MealAllowanceSeed(summaryId, employeeId, now);
    }

    private sealed record MealAllowanceSeed(Guid SummaryId, Guid EmployeeId, DateTime UpdatedAtUtc);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MealAllowancePostgreSqlCollection : ICollectionFixture<MealAllowancePostgreSqlFixture>
{
    public const string Name = "Meal allowance PostgreSQL integration";
}

public sealed class MealAllowancePostgreSqlFixture : IAsyncLifetime
{
    public const string ConnectionVariable = "VNTA_MEAL_ALLOWANCE_TEST_DB";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionVariable);
        if(string.IsNullOrWhiteSpace(configured))
            return;

        var builder = new NpgsqlConnectionStringBuilder(configured);
        if(string.IsNullOrWhiteSpace(builder.Database)
            || !builder.Database.StartsWith("vnta_meal_allowance_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionVariable} must target a disposable vnta_meal_allowance_test* database.");
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
                $"Set {ConnectionVariable} to run meal allowance PostgreSQL integration tests.");
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
public sealed class PostgreSqlMealAllowanceFactAttribute : FactAttribute
{
    public PostgreSqlMealAllowanceFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(MealAllowancePostgreSqlFixture.ConnectionVariable)))
        {
            Skip = $"Set {MealAllowancePostgreSqlFixture.ConnectionVariable} to a disposable vnta_meal_allowance_test* PostgreSQL database to run this test.";
        }
    }
}
