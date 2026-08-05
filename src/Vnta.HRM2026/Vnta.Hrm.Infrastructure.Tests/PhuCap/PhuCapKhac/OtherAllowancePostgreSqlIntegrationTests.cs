using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapKhac;

[Collection(OtherAllowancePostgreSqlCollection.Name)]
public sealed class OtherAllowancePostgreSqlIntegrationTests(OtherAllowancePostgreSqlFixture fixture)
{
    [OtherAllowancePostgreSqlFact]
    public async Task Create_rolls_back_detail_when_summary_synchronization_fails()
    {
        fixture.RequireDatabase();
        var summary = await SeedSummaryAsync();

        await using (var commandContext = fixture.CreateDbContext(new ThrowOnSecondSaveChangesInterceptor()))
        {
            var service = new DatabaseOtherAllowanceCreateService(commandContext);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new CreateOtherAllowanceRequest(
                summary.Id,
                "Hỗ trợ ăn ca",
                IsFixedAmount: true,
                AllowanceAmount: 500_000m,
                Note: null,
                RequestedBy: "integration-test")));
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.Empty(await verificationContext.PayrollOtherAllowanceRecords.ToListAsync());
        var persistedSummary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == summary.Id);
        Assert.Equal(0m, persistedSummary.OtherAllowanceAmount);
    }

    [OtherAllowancePostgreSqlFact]
    public async Task Update_raises_ef_concurrency_exception_after_an_interleaved_write()
    {
        fixture.RequireDatabase();
        var summary = await SeedSummaryAsync();
        var version = new DateTime(2026, 7, 30, 7, 0, 0, DateTimeKind.Unspecified);
        var detail = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = "Hỗ trợ điện thoại",
            IsFixedAmount = true,
            AllowanceAmount = 300_000m,
            CreatedAtUtc = version,
            CreatedBy = "integration-test",
            UpdatedAtUtc = version,
            UpdatedBy = "integration-test"
        };
        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.PayrollOtherAllowanceRecords.Add(detail);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext(new InterleavedDetailUpdateInterceptor(fixture, detail.Id)))
        {
            var service = new DatabaseOtherAllowanceUpdateService(commandContext);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.UpdateAsync(new UpdateOtherAllowanceRequest(
                detail.Id,
                "Hỗ trợ điện thoại mới",
                IsFixedAmount: true,
                AllowanceAmount: 500_000m,
                Note: "Điều chỉnh",
                OriginalUpdatedAtUtc: version,
                RequestedBy: "integration-test")));
        }

        await using var verificationContext = fixture.CreateDbContext();
        var persistedDetail = await verificationContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == detail.Id);
        Assert.Equal("Hỗ trợ điện thoại", persistedDetail.AllowanceName);
        Assert.Equal(300_000m, persistedDetail.AllowanceAmount);
        Assert.Equal("concurrent-writer", persistedDetail.UpdatedBy);
    }

    [OtherAllowancePostgreSqlFact]
    public async Task Search_normalizes_text_and_matches_allowance_name_note_and_employee_name()
    {
        fixture.RequireDatabase();
        var summary = await SeedSummaryAsync();
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        await using (var setupContext = fixture.CreateDbContext())
        {
            setupContext.PayrollOtherAllowanceRecords.AddRange(
                new PayrollOtherAllowanceRecordRow
                {
                    Id = Guid.NewGuid(), PayrollAllowanceSummaryRecordId = summary.Id,
                    AllowanceName = "Khoản target-name", IsFixedAmount = true, AllowanceAmount = 100m,
                    CreatedAtUtc = now, CreatedBy = "integration-test"
                },
                new PayrollOtherAllowanceRecordRow
                {
                    Id = Guid.NewGuid(), PayrollAllowanceSummaryRecordId = summary.Id,
                    AllowanceName = "Khoản khác", Note = "Ghi chú target-note", IsFixedAmount = true, AllowanceAmount = 200m,
                    CreatedAtUtc = now, CreatedBy = "integration-test"
                },
                new PayrollOtherAllowanceRecordRow
                {
                    Id = Guid.NewGuid(), PayrollAllowanceSummaryRecordId = summary.Id,
                    AllowanceName = "Khoản thứ ba", IsFixedAmount = true, AllowanceAmount = 300m,
                    CreatedAtUtc = now, CreatedBy = "integration-test"
                });
            await setupContext.SaveChangesAsync();
        }

        await using var commandContext = fixture.CreateDbContext();
        var service = new DatabaseOtherAllowanceQueryService(commandContext);
        var nameAndNotePage = await service.SearchPageAsync(new OtherAllowanceFilter(7, 2026, "  target-  "));
        var employeePage = await service.SearchPageAsync(new OtherAllowanceFilter(7, 2026, "  allowance  "));

        Assert.Equal(2, nameAndNotePage.TotalCount);
        Assert.Equal(300m, nameAndNotePage.TotalAllowanceAmount);
        Assert.Equal(3, employeePage.TotalCount);
        Assert.Equal(600m, employeePage.TotalAllowanceAmount);
    }

    private async Task<PayrollAllowanceSummaryRecordRow> SeedSummaryAsync()
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var department = new AttendanceDepartmentRow
        {
            Id = Guid.NewGuid(),
            Code = $"D-{Guid.NewGuid():N}"[..14],
            CenterName = "Test Center",
            DepartmentOrWorkshopName = "Test Department",
            Status = 1,
            CreatedAtUtc = now
        };
        var position = new AttendanceGatewayPositionRow
        {
            Id = Guid.NewGuid(),
            Code = $"P-{Guid.NewGuid():N}"[..14],
            Name = "Test Position",
            Status = 1,
            EmployeeCount = 1,
            CreatedAtUtc = now
        };
        var employee = new AttendanceGatewayEmployeeRow
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            PositionId = position.Id,
            EmployeeCode = $"E-{Guid.NewGuid():N}"[..14],
            FirstName = "Other",
            LastName = "Allowance",
            HireDate = now,
            Status = 1,
            IsDeleted = false,
            CreatedAtUtc = now
        };
        var summary = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            PayrollYear = 2026,
            PayrollMonth = 7,
            CreatedAtUtc = now,
            CreatedBy = "integration-test"
        };

        await using var setupContext = fixture.CreateDbContext();
        setupContext.AddRange(department, position, employee, summary);
        await setupContext.SaveChangesAsync();
        return summary;
    }

    private sealed class ThrowOnSecondSaveChangesInterceptor : SaveChangesInterceptor
    {
        private int saveCount;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref saveCount) == 2)
            {
                throw new InvalidOperationException("Forced failure while saving the summary.");
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class InterleavedDetailUpdateInterceptor(OtherAllowancePostgreSqlFixture fixture, Guid detailId)
        : SaveChangesInterceptor
    {
        private int hasWritten;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref hasWritten, 1) == 0)
            {
                await using var concurrentContext = fixture.CreateDbContext();
                var concurrentRow = await concurrentContext.PayrollOtherAllowanceRecords
                    .SingleAsync(row => row.Id == detailId, cancellationToken);
                concurrentRow.UpdatedAtUtc = concurrentRow.UpdatedAtUtc!.Value.AddTicks(1);
                concurrentRow.UpdatedBy = "concurrent-writer";
                await concurrentContext.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OtherAllowancePostgreSqlCollection : ICollectionFixture<OtherAllowancePostgreSqlFixture>
{
    public const string Name = "Other allowance PostgreSQL integration";
}

public sealed class OtherAllowancePostgreSqlFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariable = "VNTA_OTHER_ALLOWANCE_TEST_DB";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(configuredConnectionString);
        if (string.IsNullOrWhiteSpace(builder.Database)
            || !builder.Database.StartsWith("vnta_other_allowance_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must target a disposable database named vnta_other_allowance_test*.");
        }

        connectionString = builder.ConnectionString;
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (connectionString is null)
        {
            return;
        }

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    public void RequireDatabase()
    {
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} to run other-allowance PostgreSQL integration tests.");
        }
    }

    public ApplicationDbContext CreateDbContext(params IInterceptor[] interceptors)
    {
        RequireDatabase();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);
        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class OtherAllowancePostgreSqlFactAttribute : FactAttribute
{
    public OtherAllowancePostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNTA_OTHER_ALLOWANCE_TEST_DB")))
        {
            Skip = "Set VNTA_OTHER_ALLOWANCE_TEST_DB to a disposable vnta_other_allowance_test* PostgreSQL database to run these tests.";
        }
    }
}
