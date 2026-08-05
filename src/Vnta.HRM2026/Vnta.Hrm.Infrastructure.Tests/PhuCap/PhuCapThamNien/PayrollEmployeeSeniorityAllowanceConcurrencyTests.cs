using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapThamNien;

public sealed class PayrollEmployeeSeniorityAllowanceConcurrencyTests
{
    [Fact]
    public async Task Update_manual_values_requires_original_updated_at_utc()
    {
        await using var dbContext = CreateDbContext();
        var service = new DatabasePayrollEmployeeSeniorityAllowanceManualAdjustmentService(dbContext, null!, null!);

        await Assert.ThrowsAsync<PayrollEmployeeSeniorityAllowanceConflictException>(() =>
            service.UpdateManualValuesAsync(
                new UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest(
                    Guid.NewGuid(),
                    150_000m,
                    null,
                    default)));
    }

    [Fact]
    public async Task Set_lock_state_requires_original_updated_at_utc()
    {
        await using var dbContext = CreateDbContext();
        var service = new DatabasePayrollEmployeeSeniorityAllowanceLockService(dbContext, null!, null!);

        await Assert.ThrowsAsync<PayrollEmployeeSeniorityAllowanceConflictException>(() =>
            service.SetLockStateAsync(
                new SetPayrollEmployeeSeniorityAllowanceLockStateRequest(
                    Guid.NewGuid(),
                    true,
                    default)));
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"seniority-allowance-concurrency-{Guid.NewGuid():N}")
            .Options);
}
