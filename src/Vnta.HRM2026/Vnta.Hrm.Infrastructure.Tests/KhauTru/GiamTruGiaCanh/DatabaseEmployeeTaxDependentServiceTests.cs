using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.GiamTruGiaCanh;

public sealed class DatabaseEmployeeTaxDependentServiceTests
{
    [Fact]
    public void Audit_policy_masks_identity_and_tax_fields()
    {
        var policy = new AuditPolicy().GetPolicy(typeof(PayrollEmployeeTaxDependentRow));

        Assert.NotNull(policy);
        Assert.True(policy.TryGetProperty(nameof(PayrollEmployeeTaxDependentRow.DependentTaxCode), out var dependentTaxCode));
        Assert.True(policy.TryGetProperty(nameof(PayrollEmployeeTaxDependentRow.DependentIdentityNumber), out var dependentIdentityNumber));
        Assert.True(policy.TryGetProperty(nameof(PayrollEmployeeTaxDependentRow.EmployeeTaxCode), out var employeeTaxCode));
        Assert.True(policy.TryGetProperty(nameof(PayrollEmployeeTaxDependentRow.EmployeeIdentityNumber), out var employeeIdentityNumber));
        Assert.True(dependentTaxCode.IsSensitive);
        Assert.True(dependentIdentityNumber.IsSensitive);
        Assert.True(employeeTaxCode.IsSensitive);
        Assert.True(employeeIdentityNumber.IsSensitive);
    }

    [Fact]
    public async Task SaveAsync_rejects_update_without_a_concurrency_token()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = await AddEmployeeAsync(dbContext);
        var row = await AddDependentAsync(dbContext, employeeId);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => CreateService(dbContext).SaveAsync(
            CreateRequest(row, employeeId, originalUpdatedAtUtc: null)));
    }

    [Fact]
    public async Task SaveAsync_rejects_update_when_the_original_effective_period_is_locked()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = await AddEmployeeAsync(dbContext);
        var row = await AddDependentAsync(dbContext, employeeId, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1));
        dbContext.PayrollDeductionSummaryRecords.Add(new PayrollDeductionSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollYear = 2026,
            PayrollMonth = 1,
            IsLocked = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(dbContext).SaveAsync(
            CreateRequest(
                row,
                employeeId,
                row.UpdatedAtUtc,
                deductionFromMonth: new DateOnly(2027, 1, 1),
                deductionToMonth: new DateOnly(2027, 2, 1))));
    }

    [Fact]
    public async Task SaveAsync_rejects_an_invalid_effective_period()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = await AddEmployeeAsync(dbContext);
        var row = await AddDependentAsync(dbContext, employeeId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(dbContext).SaveAsync(
            CreateRequest(
                row,
                employeeId,
                row.UpdatedAtUtc,
                deductionFromMonth: new DateOnly(2026, 2, 1),
                deductionToMonth: new DateOnly(2026, 1, 1))));
    }

    [Fact]
    public async Task SaveAsync_rejects_reassigning_an_existing_dependent_to_another_employee()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = await AddEmployeeAsync(dbContext);
        var anotherEmployeeId = await AddEmployeeAsync(dbContext);
        var row = await AddDependentAsync(dbContext, employeeId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(dbContext).SaveAsync(
            CreateRequest(row, anotherEmployeeId, row.UpdatedAtUtc)));
    }

    [Fact]
    public async Task SaveAsync_preserves_country_and_address_fields_during_adjustment()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = await AddEmployeeAsync(dbContext);
        var row = await AddDependentAsync(dbContext, employeeId);
        row.CountryName = "Việt Nam";
        row.OldWardCode = "001";
        row.NewProvinceName = "Hà Nội";
        await dbContext.SaveChangesAsync();

        await CreateService(dbContext).SaveAsync(CreateRequest(
            row,
            employeeId,
            row.UpdatedAtUtc,
            countryName: "Không được ghi đè",
            oldWardCode: "999",
            newProvinceName: "Không được ghi đè"));

        var saved = await dbContext.PayrollEmployeeTaxDependents.SingleAsync(x => x.Id == row.Id);
        Assert.Equal("Việt Nam", saved.CountryName);
        Assert.Equal("001", saved.OldWardCode);
        Assert.Equal("Hà Nội", saved.NewProvinceName);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"tax-dependent-{Guid.NewGuid():N}")
            .Options);

    private static DatabaseEmployeeTaxDependentService CreateService(ApplicationDbContext dbContext) => new(dbContext);

    private static async Task<Guid> AddEmployeeAsync(ApplicationDbContext dbContext)
    {
        var id = Guid.NewGuid();
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = id,
            EmployeeCode = "NV001",
            FirstName = "An",
            LastName = "Nguyễn",
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return id;
    }

    private static async Task<PayrollEmployeeTaxDependentRow> AddDependentAsync(
        ApplicationDbContext dbContext,
        Guid employeeId,
        DateOnly? deductionFromMonth = null,
        DateOnly? deductionToMonth = null)
    {
        var row = new PayrollEmployeeTaxDependentRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            DependentFullName = "Nguyễn Văn B",
            DeductionFromMonth = deductionFromMonth,
            DeductionToMonth = deductionToMonth,
            CreatedAtUtc = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc),
            CreatedBy = "test",
            UpdatedBy = "test"
        };
        dbContext.PayrollEmployeeTaxDependents.Add(row);
        await dbContext.SaveChangesAsync();
        return row;
    }

    private static SaveEmployeeTaxDependentRequest CreateRequest(
        PayrollEmployeeTaxDependentRow row,
        Guid employeeId,
        DateTime? originalUpdatedAtUtc,
        DateOnly? deductionFromMonth = null,
        DateOnly? deductionToMonth = null,
        string? countryName = null,
        string? oldWardCode = null,
        string? newProvinceName = null) => new(
        row.Id,
        employeeId,
        null,
        null,
        row.DependentFullName,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        true,
        null,
        null,
        countryName,
        oldWardCode,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        newProvinceName,
        deductionFromMonth,
        deductionToMonth,
        null,
        originalUpdatedAtUtc,
        "test");
}
