using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruBHXHYT;

public sealed class PayrollInsuranceDeductionRefreshTests
{
    private const int PayrollMonth = 7;
    private const int PayrollYear = 2026;

    [Fact]
    public async Task Refresh_row_recalculates_only_the_requested_row_and_preserves_manual_inputs()
    {
        await using var dbContext = CreateDbContext();
        var targetSummary = CreateSummary();
        var otherSummary = CreateSummary();
        var target = CreateDetail(targetSummary.Id, insuranceSalaryBaseAmount: 10_000m);
        var other = CreateDetail(otherSummary.Id, insuranceSalaryBaseAmount: 20_000m);
        other.TotalDeductionAmount = 999m;

        dbContext.AddRange(targetSummary, otherSummary, target, other);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).RefreshAsync(
            new RefreshPayrollInsuranceDeductionRequest(PayrollMonth, PayrollYear, targetSummary.Id));

        Assert.Equal(1, result.MatchedRowCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.SkippedLockedCount);
        Assert.Equal(10_000m, target.InsuranceSalaryBaseAmount);
        Assert.Equal(.08m, target.SocialInsuranceRate);
        Assert.Equal(.015m, target.HealthInsuranceRate);
        Assert.Equal(.01m, target.UnemploymentInsuranceRate);
        Assert.Equal(800m, target.SocialInsuranceAmount);
        Assert.Equal(150m, target.HealthInsuranceAmount);
        Assert.Equal(100m, target.UnemploymentInsuranceAmount);
        Assert.Equal(1_050m, target.TotalDeductionAmount);
        Assert.Equal(1_050m, targetSummary.SocialInsuranceDeductionAmount);
        Assert.Equal(999m, other.TotalDeductionAmount);
    }

    [Fact]
    public async Task Refresh_row_skips_a_locked_detail_without_overwriting_it()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        var detail = CreateDetail(summary.Id, insuranceSalaryBaseAmount: 10_000m, isLocked: true);
        detail.TotalDeductionAmount = 123m;

        dbContext.AddRange(summary, detail);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).RefreshAsync(
            new RefreshPayrollInsuranceDeductionRequest(PayrollMonth, PayrollYear, summary.Id));

        Assert.Equal(1, result.MatchedRowCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(123m, detail.TotalDeductionAmount);
    }

    [Fact]
    public async Task Refresh_row_rounds_each_employee_insurance_component_to_whole_vnd_before_summing()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        var detail = CreateDetail(summary.Id, insuranceSalaryBaseAmount: 12_345.67m);
        dbContext.AddRange(summary, detail);
        await dbContext.SaveChangesAsync();

        await CreateService(dbContext).RefreshAsync(
            new RefreshPayrollInsuranceDeductionRequest(PayrollMonth, PayrollYear, summary.Id));

        Assert.Equal(988m, detail.SocialInsuranceAmount);
        Assert.Equal(185m, detail.HealthInsuranceAmount);
        Assert.Equal(123m, detail.UnemploymentInsuranceAmount);
        Assert.Equal(1_296m, detail.TotalDeductionAmount);
        Assert.Equal(1_296m, summary.SocialInsuranceDeductionAmount);
    }

    [Fact]
    public async Task Refresh_row_zeros_calculated_amounts_when_employee_is_not_participating()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.SocialInsuranceDeductionAmount = 1_050m;
        var detail = CreateDetail(summary.Id, insuranceSalaryBaseAmount: 10_000m);
        detail.IsParticipating = false;
        detail.SocialInsuranceAmount = 800m;
        detail.HealthInsuranceAmount = 150m;
        detail.UnemploymentInsuranceAmount = 100m;
        detail.TotalDeductionAmount = 1_050m;
        dbContext.AddRange(summary, detail);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).RefreshAsync(
            new RefreshPayrollInsuranceDeductionRequest(PayrollMonth, PayrollYear, summary.Id));

        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0m, detail.TotalInsuranceRate);
        Assert.Equal(0m, detail.SocialInsuranceAmount);
        Assert.Equal(0m, detail.HealthInsuranceAmount);
        Assert.Equal(0m, detail.UnemploymentInsuranceAmount);
        Assert.Equal(0m, detail.TotalDeductionAmount);
        Assert.Equal(0m, summary.SocialInsuranceDeductionAmount);
    }

    [Fact]
    public async Task Refresh_row_skips_a_locked_summary_without_overwriting_detail_or_summary()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.IsLocked = true;
        summary.SocialInsuranceDeductionAmount = 123m;
        var detail = CreateDetail(summary.Id, insuranceSalaryBaseAmount: 10_000m);
        detail.TotalDeductionAmount = 456m;
        dbContext.AddRange(summary, detail);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).RefreshAsync(
            new RefreshPayrollInsuranceDeductionRequest(PayrollMonth, PayrollYear, summary.Id));

        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(456m, detail.TotalDeductionAmount);
        Assert.Equal(123m, summary.SocialInsuranceDeductionAmount);
    }

    [Fact]
    public async Task Refresh_row_rejects_an_identifier_outside_the_requested_period()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary(payrollMonth: 6);
        var detail = CreateDetail(summary.Id, insuranceSalaryBaseAmount: 10_000m);
        dbContext.AddRange(summary, detail);
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(dbContext).RefreshAsync(
                new RefreshPayrollInsuranceDeductionRequest(PayrollMonth, PayrollYear, summary.Id)));

        Assert.Equal("Không tìm thấy dòng khấu trừ BHXH-YT thuộc kỳ lương đã chọn.", exception.Message);
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"insurance-deduction-refresh-{Guid.NewGuid():N}")
            .Options);

    private static DatabasePayrollInsuranceDeductionService CreateService(ApplicationDbContext dbContext)
    {
        var auditScope = new AsyncLocalAuditScope();
        return new DatabasePayrollInsuranceDeductionService(
            dbContext,
            auditScope,
            new AuditedMutation(dbContext, auditScope));
    }

    private static PayrollDeductionSummaryRecordRow CreateSummary(int payrollMonth = PayrollMonth) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = checked((short)payrollMonth),
            PayrollYear = PayrollYear,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        };

    private static PayrollDeductionInsuranceRecordRow CreateDetail(
        Guid summaryId,
        decimal insuranceSalaryBaseAmount,
        bool isLocked = false) =>
        new()
        {
            PayrollDeductionSummaryRecordId = summaryId,
            InsuranceSalaryBaseAmount = insuranceSalaryBaseAmount,
            SocialInsuranceRate = .08m,
            HealthInsuranceRate = .015m,
            UnemploymentInsuranceRate = .01m,
            IsParticipating = true,
            IsLocked = isLocked,
            CreatedAtUtc = DateTime.UtcNow
        };
}
