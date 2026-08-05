using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruPhiCongDoan;

public sealed class PayrollUnionFeeDeductionReadServiceTests
{
    [Fact]
    public async Task SearchAsync_returns_only_supported_period_rows_and_reports_total_count()
    {
        await using var dbContext = CreateDbContext();
        var employee = CreateEmployee("E001", "An", "Nguyen");
        dbContext.Employees.Add(employee);

        var unsupportedSummary = CreateSummary(employee.Id, 5, 2026, 50_000m);
        var juneSummary = CreateSummary(employee.Id, 6, 2026, 60_000m);
        var julySummary = CreateSummary(employee.Id, 7, 2026, 70_000m);
        dbContext.PayrollDeductionSummaryRecords.AddRange(unsupportedSummary, juneSummary, julySummary);
        dbContext.PayrollDeductionUnionFeeRecords.AddRange(
            CreateDetail(unsupportedSummary.Id, 1m),
            CreateDetail(juneSummary.Id, 1m),
            CreateDetail(julySummary.Id, 1m));
        await dbContext.SaveChangesAsync();

        var page = await CreateService(dbContext).SearchAsync(
            new PayrollUnionFeeDeductionFilter(null, null, null, Skip: 0, Take: 1));

        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(7, page.Items[0].PayrollMonth);
        Assert.Equal(2026, page.Items[0].PayrollYear);
        Assert.Equal(70_000m, page.Items[0].DeductionAmount);
    }

    [Fact]
    public async Task SearchAsync_applies_requested_period_and_maps_employee_metadata()
    {
        await using var dbContext = CreateDbContext();
        var department = CreateDepartment();
        var position = CreatePosition();
        var employee = CreateEmployee("E002", "Binh", "Tran", department.Id, position.Id);
        dbContext.AddRange(department, position, employee);

        var targetSummary = CreateSummary(employee.Id, 6, 2026, 125_000m);
        var otherSummary = CreateSummary(employee.Id, 7, 2026, 140_000m);
        dbContext.PayrollDeductionSummaryRecords.AddRange(targetSummary, otherSummary);
        dbContext.PayrollDeductionUnionFeeRecords.AddRange(
            CreateDetail(targetSummary.Id, 1m, isLocked: true),
            CreateDetail(otherSummary.Id, 1m));
        await dbContext.SaveChangesAsync();

        var page = await CreateService(dbContext).SearchAsync(
            new PayrollUnionFeeDeductionFilter(6, 2026, null));

        var row = Assert.Single(page.Items);
        Assert.Equal("E002", row.EmployeeCode);
        Assert.Equal("Tran Binh", row.EmployeeName);
        Assert.Equal("Tổ 1", row.DepartmentName);
        Assert.Equal("Nhân viên", row.PositionName);
        Assert.Equal(125_000m, row.DeductionAmount);
        Assert.True(row.IsLocked);
        Assert.Equal(1, page.TotalCount);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"union-fee-read-{Guid.NewGuid():N}")
            .Options);

    private static DatabasePayrollUnionFeeDeductionReadService CreateService(ApplicationDbContext dbContext) => new(dbContext);

    private static AttendanceGatewayEmployeeRow CreateEmployee(
        string employeeCode,
        string firstName,
        string lastName,
        Guid? departmentId = null,
        Guid? positionId = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeCode = employeeCode,
        FirstName = firstName,
        LastName = lastName,
        DepartmentId = departmentId ?? Guid.Empty,
        PositionId = positionId ?? Guid.Empty,
        HireDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        Status = 1,
        IsDeleted = false,
        CreatedAtUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static AttendanceDepartmentRow CreateDepartment() => new()
    {
        Id = Guid.NewGuid(),
        Code = "DP-001",
        DepartmentOrWorkshopName = "Khối văn phòng",
        TeamName = "Đội A",
        GroupName = "Tổ 1",
        CenterName = "Trung tâm",
        Status = 1,
        CreatedAtUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static AttendanceGatewayPositionRow CreatePosition() => new()
    {
        Id = Guid.NewGuid(),
        Code = "CV-001",
        Name = "Nhân viên",
        Status = 1,
        EmployeeCount = 1,
        CreatedAtUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static PayrollDeductionSummaryRecordRow CreateSummary(
        Guid employeeId,
        short payrollMonth,
        short payrollYear,
        decimal unionFeeAmount) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = employeeId,
        PayrollMonth = payrollMonth,
        PayrollYear = payrollYear,
        UnionFeeDeductionAmount = unionFeeAmount,
        IsLocked = false,
        CreatedAtUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = "test"
    };

    private static PayrollDeductionUnionFeeRecordRow CreateDetail(
        Guid summaryId,
        decimal amount,
        bool isLocked = false) => new()
    {
        PayrollDeductionSummaryRecordId = summaryId,
        DeductionAmount = amount,
        IsLocked = isLocked,
        CreatedAtUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
