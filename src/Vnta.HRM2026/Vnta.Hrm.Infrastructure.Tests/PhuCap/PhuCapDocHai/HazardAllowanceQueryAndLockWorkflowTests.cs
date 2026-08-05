using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapDocHai;

/// <summary>Business characterizations for read, manual-edit, and lock workflows that run without an external database.</summary>
public sealed class HazardAllowanceQueryAndLockWorkflowTests
{
    [Fact]
    public async Task Query_filters_by_period_lock_and_summary_bucket_then_exports_every_matching_row_in_stable_order()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, "NV-02", payrollMonth: 7, isLocked: false, isEligible: true, amount: 600_000m);
        await SeedAsync(dbContext, "NV-01", payrollMonth: 7, isLocked: true, isEligible: false, amount: 0m, exclusionReason: "Ngoài diện");
        await SeedAsync(dbContext, "NV-00", payrollMonth: 8, isLocked: false, isEligible: true, amount: 600_000m);
        await dbContext.SaveChangesAsync();

        var projection = new HazardAllowanceReadProjection(dbContext, new HazardAllowanceRequestValidator());
        var firstPage = await projection.SearchPageAsync(new HazardAllowanceFilter(
            7, 2026, HazardAllowanceLockState.All, null, Take: 1), default);
        var eligible = await projection.SearchPageAsync(new HazardAllowanceFilter(
            7, 2026, HazardAllowanceLockState.All, null, SummaryBucket: HazardAllowanceSummaryBucket.Eligible), default);
        var open = await projection.SearchPageAsync(new HazardAllowanceFilter(
            7, 2026, HazardAllowanceLockState.Open, null), default);
        var export = await projection.ExportAsync(new HazardAllowanceFilter(
            7, 2026, HazardAllowanceLockState.All, null, Take: 1), default);

        Assert.Equal(2, firstPage.TotalCount);
        Assert.Single(firstPage.Rows);
        Assert.Equal("NV-01", firstPage.Rows[0].EmployeeCode);
        Assert.Single(eligible.Rows);
        Assert.Equal("NV-02", eligible.Rows[0].EmployeeCode);
        Assert.Single(open.Rows);
        Assert.Equal("NV-02", open.Rows[0].EmployeeCode);
        Assert.Equal(["NV-01", "NV-02"], export.Select(row => row.EmployeeCode));
    }

    [Fact]
    public async Task Query_clamps_invalid_paging_and_summary_counts_the_same_period_snapshot_buckets()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, "NV-02", payrollMonth: 7, isLocked: false, isEligible: true, amount: 600_000m);
        await SeedAsync(dbContext, "NV-01", payrollMonth: 7, isLocked: true, isEligible: false, amount: 0m, exclusionReason: "Ngoài diện");
        await dbContext.SaveChangesAsync();

        var projection = new HazardAllowanceReadProjection(dbContext, new HazardAllowanceRequestValidator());
        var page = await projection.SearchPageAsync(new HazardAllowanceFilter(
            7, 2026, HazardAllowanceLockState.All, null, Take: 0, Skip: -20), default);
        var summary = await projection.GetSummaryAsync(new HazardAllowanceFilter(
            7, 2026, HazardAllowanceLockState.All, null), default);

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Rows);
        Assert.Equal("NV-01", page.Rows[0].EmployeeCode);
        Assert.Equal(new HazardAllowanceSummaryDto(2, 1, 1, 1, 1), summary);
    }

    [Fact]
    public async Task Query_returns_the_full_department_path_for_grid_and_export()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(
            dbContext,
            "NV-01",
            payrollMonth: 7,
            isLocked: false,
            isEligible: true,
            amount: 600_000m,
            centerName: "Sản xuất",
            departmentOrWorkshopName: "Phòng kế toán",
            teamName: "Kho vật tư",
            groupName: "Nhóm kiểm kê");
        await dbContext.SaveChangesAsync();

        var projection = new HazardAllowanceReadProjection(dbContext, new HazardAllowanceRequestValidator());
        var row = Assert.Single((await projection.ExportAsync(new HazardAllowanceFilter(
            7, 2026, HazardAllowanceLockState.All, null), default)));

        Assert.Equal("Sản xuất / Phòng kế toán / Kho vật tư / Nhóm kiểm kê", row.DepartmentName);
    }

    [Fact]
    public async Task Manual_adjustment_rejects_locked_snapshot_and_preserves_detail_and_summary()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAsync(dbContext, "NV-01", payrollMonth: 7, isLocked: true, isEligible: true, amount: 600_000m);
        await dbContext.SaveChangesAsync();

        var service = new DatabaseHazardAllowanceManualAdjustmentService(dbContext, new HazardAllowanceManualAdjustmentPolicy(), new HazardAllowanceRequestValidator());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateManualValuesAsync(
            new UpdateHazardAllowanceManualValuesRequest(
                seed.SummaryId, 10m, 0m, 0m, 300_000m, true, null,
                seed.Timestamp, seed.Timestamp, "payroll-admin")));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(600_000m, detail.HazardAllowanceAmount);
        Assert.Equal(600_000m, summary.HazardAllowanceAmount);
    }

    [Fact]
    public async Task Manual_adjustment_rejects_stale_versions_before_changing_either_projection()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAsync(dbContext, "NV-01", payrollMonth: 7, isLocked: false, isEligible: true, amount: 600_000m);
        await dbContext.SaveChangesAsync();

        var service = new DatabaseHazardAllowanceManualAdjustmentService(dbContext, new HazardAllowanceManualAdjustmentPolicy(), new HazardAllowanceRequestValidator());
        await Assert.ThrowsAsync<HazardAllowanceConflictException>(() => service.UpdateManualValuesAsync(
            new UpdateHazardAllowanceManualValuesRequest(
                seed.SummaryId, 10m, 0m, 0m, 300_000m, true, null,
                seed.Timestamp.AddTicks(-1), seed.Timestamp, "payroll-admin")));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(2m, detail.QualifiedWorkdayCount);
        Assert.Equal(600_000m, detail.HazardAllowanceAmount);
        Assert.Equal(600_000m, summary.HazardAllowanceAmount);
    }

    [Fact]
    public async Task Batch_lock_and_unlock_scope_selected_rows_or_the_whole_period_and_are_idempotent()
    {
        await using var dbContext = CreateDbContext();
        var first = await SeedAsync(dbContext, "NV-01", payrollMonth: 7, isLocked: false, isEligible: true, amount: 600_000m);
        var second = await SeedAsync(dbContext, "NV-02", payrollMonth: 7, isLocked: false, isEligible: true, amount: 300_000m);
        await SeedAsync(dbContext, "NV-03", payrollMonth: 8, isLocked: false, isEligible: true, amount: 600_000m);
        await dbContext.SaveChangesAsync();
        var service = new DatabaseHazardAllowanceLockService(dbContext, new HazardAllowanceLockStatePolicy(), new HazardAllowanceRequestValidator());

        var selected = await service.SetLockStateBatchAsync(
            new SetHazardAllowanceBatchLockStateRequest(2026, 7, true, [first.SummaryId], "payroll-admin"));
        var repeated = await service.SetLockStateBatchAsync(
            new SetHazardAllowanceBatchLockStateRequest(2026, 7, true, [first.SummaryId], "payroll-admin"));

        Assert.False(await IsLockedAsync(dbContext, first.SummaryId));
        Assert.True(await IsHazardDetailLockedAsync(dbContext, first.SummaryId));

        var wholePeriod = await service.SetLockStateBatchAsync(
            new SetHazardAllowanceBatchLockStateRequest(2026, 7, false, null, "payroll-admin"));

        Assert.Equal(new SetHazardAllowanceBatchLockStateResult(2026, 7, 1, 1), selected);
        Assert.Equal(new SetHazardAllowanceBatchLockStateResult(2026, 7, 1, 0), repeated);
        Assert.Equal(new SetHazardAllowanceBatchLockStateResult(2026, 7, 2, 1), wholePeriod);
        Assert.False(await IsLockedAsync(dbContext, first.SummaryId));
        Assert.False(await IsLockedAsync(dbContext, second.SummaryId));
        Assert.False(await IsHazardDetailLockedAsync(dbContext, first.SummaryId));
        Assert.False(await IsHazardDetailLockedAsync(dbContext, second.SummaryId));
    }

    [Fact]
    public async Task Entitlement_batch_changes_only_selected_unlocked_rows()
    {
        await using var dbContext = CreateDbContext();
        var first = await SeedAsync(dbContext, "NV-01", payrollMonth: 7, isLocked: false, isEligible: true, amount: 600_000m);
        var second = await SeedAsync(dbContext, "NV-02", payrollMonth: 7, isLocked: false, isEligible: true, amount: 300_000m);
        await dbContext.SaveChangesAsync();
        var service = new DatabaseHazardAllowanceEntitlementService(dbContext, new HazardAllowanceRequestValidator());

        var result = await service.SetEntitlementBatchAsync(
            new SetHazardAllowanceEntitlementBatchRequest(
                false,
                [new HazardAllowanceEntitlementTarget(first.SummaryId, first.Timestamp, first.Timestamp)],
                "payroll-admin"));

        Assert.Equal(new SetHazardAllowanceEntitlementBatchResult(1, 1), result);
        Assert.False(await dbContext.PayrollHazardAllowanceRecords
            .Where(row => row.PayrollAllowanceSummaryRecordId == first.SummaryId)
            .Select(row => row.IsEligibleForAllowance)
            .SingleAsync());
        Assert.Equal(0m, await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.Id == first.SummaryId)
            .Select(row => row.HazardAllowanceAmount)
            .SingleAsync());
        Assert.True(await dbContext.PayrollHazardAllowanceRecords
            .Where(row => row.PayrollAllowanceSummaryRecordId == second.SummaryId)
            .Select(row => row.IsEligibleForAllowance)
            .SingleAsync());
        Assert.Equal(300_000m, await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.Id == second.SummaryId)
            .Select(row => row.HazardAllowanceAmount)
            .SingleAsync());
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"hazard-allowance-workflow-{Guid.NewGuid():N}")
            .Options);

    private static async Task<HazardSeed> SeedAsync(
        ApplicationDbContext dbContext,
        string employeeCode,
        short payrollMonth,
        bool isLocked,
        bool isEligible,
        decimal amount,
        string? exclusionReason = null,
        string? centerName = null,
        string? departmentOrWorkshopName = null,
        string? teamName = null,
        string? groupName = null)
    {
        var timestamp = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Unspecified);
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        Guid? departmentId = null;
        if (centerName is not null || departmentOrWorkshopName is not null || teamName is not null || groupName is not null)
        {
            departmentId = Guid.NewGuid();
            dbContext.Departments.Add(new AttendanceDepartmentRow
            {
                Id = departmentId.Value,
                Code = "PB-01",
                CenterName = centerName ?? string.Empty,
                DepartmentOrWorkshopName = departmentOrWorkshopName ?? string.Empty,
                TeamName = teamName,
                GroupName = groupName,
                CreatedAtUtc = timestamp
            });
        }
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = employeeId,
            EmployeeCode = employeeCode,
            FirstName = "Test",
            LastName = employeeCode,
            DepartmentId = departmentId ?? Guid.Empty,
            IsDeleted = false,
            CreatedAtUtc = timestamp
        });
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = employeeId,
            PayrollMonth = payrollMonth,
            PayrollYear = 2026,
            HazardAllowanceAmount = amount,
            IsLocked = isLocked,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
            CreatedBy = "seed",
            UpdatedBy = "seed"
        });
        dbContext.PayrollHazardAllowanceRecords.Add(new PayrollHazardAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            QualifiedWorkdayCount = 2m,
            LateEarlyDeductionDays = 0m,
            PayableWorkdayCount = 2m,
            HazardAllowancePerDay = 0m,
            HazardAllowanceAmount = amount,
            IsEligibleDepartment = isEligible,
            IsEligibleForAllowance = isEligible,
            ExclusionReason = exclusionReason,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
            CreatedBy = "seed",
            UpdatedBy = "seed"
        });
        return await Task.FromResult(new HazardSeed(summaryId, timestamp));
    }

    private static Task<bool> IsLockedAsync(ApplicationDbContext dbContext, Guid summaryId) =>
        dbContext.PayrollAllowanceSummaryRecords.Where(row => row.Id == summaryId).Select(row => row.IsLocked).SingleAsync();

    private static Task<bool> IsHazardDetailLockedAsync(ApplicationDbContext dbContext, Guid summaryId) =>
        dbContext.PayrollHazardAllowanceRecords.Where(row => row.PayrollAllowanceSummaryRecordId == summaryId).Select(row => row.IsLocked).SingleAsync();

    private sealed record HazardSeed(Guid SummaryId, DateTime Timestamp);
}
