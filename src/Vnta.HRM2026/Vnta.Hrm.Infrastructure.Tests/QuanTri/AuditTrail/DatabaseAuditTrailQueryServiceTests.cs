using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.QuanTri.AuditTrail;

public sealed class DatabaseAuditTrailQueryServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(DatabaseAuditTrailQueryService.MaxPageSize + 1)]
    public async Task GetPageAsync_rejects_page_size_outside_the_bounded_contract(int pageSize)
    {
        await using var dbContext = CreateDbContext();
        var service = new DatabaseAuditTrailQueryService(dbContext);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetPageAsync(new AuditEventFilter(PageSize: pageSize), AuditReadAccess.Masked));
    }

    [Fact]
    public async Task GetPageAsync_rejects_a_time_window_larger_than_the_audit_policy()
    {
        await using var dbContext = CreateDbContext();
        var service = new DatabaseAuditTrailQueryService(dbContext);
        var fromUtc = DateTimeOffset.UtcNow;
        var filter = new AuditEventFilter(
            FromUtc: fromUtc,
            ToUtc: fromUtc.Add(DatabaseAuditTrailQueryService.MaxTimeWindow).AddTicks(1));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetPageAsync(filter, AuditReadAccess.Masked));
    }

    [Fact]
    public void Masked_access_never_receives_sensitive_property_displays()
    {
        var change = new DatabaseAuditTrailQueryService.AuditPropertyChangeProjection(
            "BaseSalary",
            "Base salary",
            "10,000,000",
            "11,000,000",
            IsSensitive: true);

        var result = DatabaseAuditTrailQueryService.ToPropertyChangeDto(change, AuditReadAccess.Masked);

        Assert.True(result.IsSensitive);
        Assert.True(result.Changed);
        Assert.Null(result.OldDisplay);
        Assert.Null(result.NewDisplay);
    }

    [Fact]
    public void Sensitive_read_access_receives_the_stored_sensitive_displays()
    {
        var change = new DatabaseAuditTrailQueryService.AuditPropertyChangeProjection(
            "BaseSalary",
            "Base salary",
            "10,000,000",
            "11,000,000",
            IsSensitive: true);

        var result = DatabaseAuditTrailQueryService.ToPropertyChangeDto(
            change,
            new AuditReadAccess(CanReadSensitiveValues: true));

        Assert.Equal("10,000,000", result.OldDisplay);
        Assert.Equal("11,000,000", result.NewDisplay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(DatabaseAuditTrailQueryService.MaxContextTake + 1)]
    public async Task GetContextAsync_rejects_take_outside_the_bounded_contract(int take)
    {
        await using var dbContext = CreateDbContext();
        var service = new DatabaseAuditTrailQueryService(dbContext);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetContextAsync(Guid.NewGuid(), AuditReadAccess.Masked, take));
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().Options);
}
