using Microsoft.EntityFrameworkCore;
using Xunit;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapDocHai;

public sealed class HazardAllowanceRecalculateTests
{
    private const int PayrollMonth = 7;
    private const int PayrollYear = 2026;

    [Fact]
    public async Task Hazard_detail_timestamp_is_configured_as_a_concurrency_token()
    {
        await using var dbContext = CreateDbContext();

        var entityType = dbContext.Model.FindEntityType(typeof(PayrollHazardAllowanceRecordRow));
        var updatedAtProperty = entityType?.FindProperty(nameof(PayrollHazardAllowanceRecordRow.UpdatedAtUtc));

        Assert.NotNull(updatedAtProperty);
        Assert.True(updatedAtProperty!.IsConcurrencyToken);
    }

    [Theory]
    [InlineData("=SUM(A1:A2)", "\"'=SUM(A1:A2)\"")]
    [InlineData("+cmd", "\"'+cmd\"")]
    [InlineData("-1+2", "\"'-1+2\"")]
    [InlineData("@command", "\"'@command\"")]
    [InlineData("Nguyen \"Van\" A", "\"Nguyen \"\"Van\"\" A\"")]
    public void Csv_escape_neutralizes_formula_prefixes_without_changing_ordinary_csv_escaping(
        string value,
        string expected)
    {
        Assert.Equal(expected, DatabaseHazardAllowanceExportJobService.Escape(value));
    }

    [Fact]
    public async Task Recalculate_multiplies_ctl_by_the_fixed_rate_without_rounding()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 25, nonHazardLateMinutes: 30);

        var result = await CreateRefreshService(dbContext)
            .RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(25m, detail.QualifiedWorkdayCount);
        Assert.Equal(0.0625m, detail.LateEarlyDeductionDays);
        Assert.Equal(24.9375m, detail.PayableWorkdayCount);
        Assert.Equal(7_700m, detail.HazardAllowancePerDay);
        Assert.Equal(192_018.75m, detail.HazardAllowanceAmount);
        Assert.Equal(detail.HazardAllowanceAmount, summary.HazardAllowanceAmount);
        Assert.Equal(seed.SummaryId, detail.PayrollAllowanceSummaryRecordId);
    }

    [Theory]
    [InlineData(24, 184_800)]
    [InlineData(22, 169_400)]
    public async Task Recalculate_uses_the_same_fixed_rate_for_every_positive_ctl(int hazardWorkdayCount, decimal expectedAmount)
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: hazardWorkdayCount);

        await CreateRefreshService(dbContext)
            .RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        Assert.Equal(expectedAmount, detail.HazardAllowanceAmount);
    }

    [Fact]
    public async Task Recalculate_counts_every_hazard_code_regardless_of_day_type()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(
            dbContext,
            standardWorkdays: 1m,
            hazardWorkdayCount: 1,
            hazardDayType: "Ngày nghỉ");

        await CreateRefreshService(dbContext)
            .RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        Assert.Equal(1m, detail.QualifiedWorkdayCount);
        Assert.Equal(7_700m, detail.HazardAllowanceAmount);
    }

    [Fact]
    public async Task Recalculate_does_not_apply_an_unlisted_kp_zeroing_rule()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 26, hasUnexcusedAbsence: true);

        await CreateRefreshService(dbContext)
            .RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(26m, detail.QualifiedWorkdayCount);
        Assert.Equal(26m, detail.PayableWorkdayCount);
        Assert.Equal(200_200m, detail.HazardAllowanceAmount);
        Assert.Equal(200_200m, summary.HazardAllowanceAmount);
    }

    [Fact]
    public async Task Recalculate_skips_a_locked_summary_without_overwriting_its_detail_or_total()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 26, isLocked: true);
        dbContext.PayrollHazardAllowanceRecords.Add(new PayrollHazardAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = seed.SummaryId,
            QualifiedWorkdayCount = 1m,
            LateEarlyDeductionDays = 0m,
            PayableWorkdayCount = 1m,
            HazardAllowancePerDay = 7_700m,
            HazardAllowanceAmount = 7_700m,
            IsEligibleDepartment = true,
            IsEligibleForAllowance = true,
            CreatedAtUtc = seed.Timestamp,
            CreatedBy = "seed"
        });
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        summary.HazardAllowanceAmount = 7_700m;
        await dbContext.SaveChangesAsync();

        var result = await CreateRefreshService(dbContext)
            .RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(7_700m, detail.HazardAllowanceAmount);
        Assert.Equal(7_700m, summary.HazardAllowanceAmount);
    }

    [Fact]
    public async Task Recalculate_is_idempotent_when_the_existing_snapshot_and_summary_are_current()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 26);
        var service = CreateRefreshService(dbContext);

        await service.RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));
        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        var detailUpdatedAtUtc = detail.UpdatedAtUtc;
        var summaryUpdatedAtUtc = summary.UpdatedAtUtc;

        var result = await service.RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(detailUpdatedAtUtc, detail.UpdatedAtUtc);
        Assert.Equal(summaryUpdatedAtUtc, summary.UpdatedAtUtc);
    }

    [Fact]
    public async Task Recalculate_syncs_a_stale_summary_when_the_existing_detail_is_current()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 26);
        var service = CreateRefreshService(dbContext);

        await service.RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));
        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        var detailUpdatedAtUtc = detail.UpdatedAtUtc;
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        summary.HazardAllowanceAmount = 0m;
        await dbContext.SaveChangesAsync();

        var result = await service.RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(200_200m, summary.HazardAllowanceAmount);
        Assert.Equal(detailUpdatedAtUtc, detail.UpdatedAtUtc);
    }

    [Fact]
    public async Task Recalculate_for_one_summary_row_does_not_refresh_other_employees_in_the_same_period()
    {
        await using var dbContext = CreateDbContext();
        var requested = await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 26);
        var outsideScope = await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 26);

        var result = await CreateRefreshService(dbContext).RefreshAsync(
            new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test", requested.SummaryId));

        Assert.Equal(1, result.TotalSummaryRows);
        Assert.Equal(1, result.CreatedCount);
        Assert.Single(await dbContext.PayrollHazardAllowanceRecords.ToListAsync());
        Assert.Equal(requested.SummaryId, (await dbContext.PayrollHazardAllowanceRecords.SingleAsync()).PayrollAllowanceSummaryRecordId);
        Assert.Equal(0m, await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.Id == outsideScope.SummaryId)
            .Select(row => row.HazardAllowanceAmount)
            .SingleAsync());
    }

    [Fact]
    public async Task Recalculate_preserves_user_selected_entitlement_and_restores_calculated_amount_when_reenabled()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 26);
        var refreshService = CreateRefreshService(dbContext);
        await refreshService.RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        var entitlementService = CreateEntitlementService(dbContext);
        var excluded = await entitlementService.SetEntitlementBatchAsync(
            new SetHazardAllowanceEntitlementBatchRequest(
                false,
                [new HazardAllowanceEntitlementTarget(
                    summary.Id,
                    detail.UpdatedAtUtc ?? detail.CreatedAtUtc,
                    summary.UpdatedAtUtc ?? summary.CreatedAtUtc)],
                "test"));

        Assert.Equal(new SetHazardAllowanceEntitlementBatchResult(1, 1), excluded);
        await refreshService.RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));
        detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.False(detail.IsEligibleForAllowance);
        Assert.Equal(0m, detail.HazardAllowanceAmount);
        Assert.Equal(0m, summary.HazardAllowanceAmount);

        var included = await entitlementService.SetEntitlementBatchAsync(
            new SetHazardAllowanceEntitlementBatchRequest(
                true,
                [new HazardAllowanceEntitlementTarget(
                    summary.Id,
                    detail.UpdatedAtUtc ?? detail.CreatedAtUtc,
                    summary.UpdatedAtUtc ?? summary.CreatedAtUtc)],
                "test"));

        Assert.Equal(new SetHazardAllowanceEntitlementBatchResult(1, 1), included);
        await refreshService.RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));
        detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.True(detail.IsEligibleForAllowance);
        Assert.Equal(200_200m, detail.HazardAllowanceAmount);
        Assert.Equal(200_200m, summary.HazardAllowanceAmount);
    }

    [Fact]
    public async Task Manual_adjustment_updates_detail_and_summary_in_one_save_flow()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext, standardWorkdays: 26m, hazardWorkdayCount: 26);

        await CreateRefreshService(dbContext)
            .RefreshAsync(new RefreshHazardAllowanceRequest(PayrollMonth, PayrollYear, "test"));

        var detail = await dbContext.PayrollHazardAllowanceRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        var result = await CreateManualAdjustmentService(dbContext).UpdateManualValuesAsync(
            new UpdateHazardAllowanceManualValuesRequest(
                summary.Id,
                QualifiedWorkdayCount: 20m,
                LateEarlyDeductionDays: 0.125m,
                HazardAllowancePerDay: 0m,
                HazardAllowanceAmount: 300_000m,
                IsEligibleDepartment: true,
                ExclusionReason: null,
                OriginalDetailUpdatedAtUtc: detail.UpdatedAtUtc ?? detail.CreatedAtUtc,
                OriginalSummaryUpdatedAtUtc: summary.UpdatedAtUtc ?? summary.CreatedAtUtc,
                RequestedBy: "test"));

        Assert.Equal(19.875m, result.PayableWorkdayCount);
        Assert.Equal(19.875m, (await dbContext.PayrollHazardAllowanceRecords.SingleAsync()).PayableWorkdayCount);
        Assert.Equal(300_000m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync()).HazardAllowanceAmount);
    }

    private static async Task<HazardAllowanceSeed> SeedAsync(
        ApplicationDbContext dbContext,
        decimal standardWorkdays,
        int hazardWorkdayCount,
        int nonHazardLateMinutes = 0,
        bool hasUnexcusedAbsence = false,
        bool isLocked = false,
        string hazardDayType = "Ngày thường")
    {
        var timestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        var hazardStatusId = Guid.NewGuid();
        var kpStatusId = Guid.NewGuid();
        var normalStatusId = Guid.NewGuid();
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = employeeId,
            EmployeeCode = $"E-{employeeId:N}",
            FirstName = "Hazard",
            LastName = "Test",
            DepartmentId = departmentId,
            IsDeleted = false,
            CreatedAtUtc = timestamp
        });
        dbContext.Departments.Add(new AttendanceDepartmentRow
        {
            Id = departmentId,
            Code = "PX-DH",
            CenterName = "Sản xuất",
            DepartmentOrWorkshopName = "Phân xưởng độc hại",
            Status = 1,
            CreatedAtUtc = timestamp
        });
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = employeeId,
            PayrollMonth = PayrollMonth,
            PayrollYear = PayrollYear,
            IsLocked = isLocked,
            CreatedAtUtc = timestamp,
            CreatedBy = "seed"
        });
        dbContext.BasicSalaryRecords.Add(new BasicSalaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollMonth = PayrollMonth,
            PayrollYear = PayrollYear,
            BasicSalary = 10_000_000m,
            StandardWorkingDays = standardWorkdays,
            DailySalary = 384_615m,
            HourlySalary = 48_077m,
            CreatedAtUtc = timestamp
        });
        dbContext.AttendanceStatusCodes.AddRange(
            new AttendanceStatusCodeRow { Id = hazardStatusId, Code = "DH", Name = "Độc hại", PhuCapDocHai = true, IsActive = true, CreatedAtUtc = timestamp },
            new AttendanceStatusCodeRow { Id = kpStatusId, Code = "KP", Name = "Nghỉ không phép", IsActive = true, CreatedAtUtc = timestamp },
            new AttendanceStatusCodeRow { Id = normalStatusId, Code = "N", Name = "Bình thường", IsActive = true, CreatedAtUtc = timestamp });

        for(var day = 1; day <= hazardWorkdayCount; day++)
        {
            dbContext.AttendanceWorkdaySummaries.Add(CreateWorkday(employeeId, new DateOnly(PayrollYear, PayrollMonth, day), hazardStatusId, timestamp, dayType: hazardDayType));
        }

        if(nonHazardLateMinutes > 0)
        {
            dbContext.AttendanceWorkdaySummaries.Add(CreateWorkday(employeeId, new DateOnly(PayrollYear, PayrollMonth, 27), normalStatusId, timestamp, nonHazardLateMinutes));
        }

        if(hasUnexcusedAbsence)
        {
            dbContext.AttendanceWorkdaySummaries.Add(CreateWorkday(employeeId, new DateOnly(PayrollYear, PayrollMonth, 28), kpStatusId, timestamp));
        }

        await dbContext.SaveChangesAsync();
        return new HazardAllowanceSeed(summaryId, timestamp);
    }

    private static AttendanceWorkdaySummaryRow CreateWorkday(
        Guid employeeId,
        DateOnly workDate,
        Guid statusCodeId,
        DateTime timestamp,
        int lateMinutes = 0,
        string dayType = "Ngày thường") => new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = workDate,
            DayType = dayType,
            CodeKetQuaTinhCongId = statusCodeId,
            LateMinutes = lateMinutes,
            ComputedAtUtc = timestamp,
            CreatedAtUtc = timestamp
        };

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"hazard-allowance-recalculate-{Guid.NewGuid():N}")
            .Options);

    private static DatabaseHazardAllowanceRefreshService CreateRefreshService(ApplicationDbContext dbContext) =>
        new(
            dbContext,
            new HazardAllowanceCalculationPolicy(),
            new HazardAllowanceWorkdayMetricsCalculator(),
            new HazardAllowanceRequestValidator());

    private static DatabaseHazardAllowanceManualAdjustmentService CreateManualAdjustmentService(ApplicationDbContext dbContext) =>
        new(dbContext, new HazardAllowanceManualAdjustmentPolicy(), new HazardAllowanceRequestValidator());

    private static DatabaseHazardAllowanceEntitlementService CreateEntitlementService(ApplicationDbContext dbContext) =>
        new(dbContext, new HazardAllowanceRequestValidator());

    private sealed record HazardAllowanceSeed(Guid SummaryId, DateTime Timestamp);
}
