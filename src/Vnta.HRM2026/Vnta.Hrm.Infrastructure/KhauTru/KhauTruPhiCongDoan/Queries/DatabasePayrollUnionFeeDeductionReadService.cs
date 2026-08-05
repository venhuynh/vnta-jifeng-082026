using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruPhiCongDoan;

public sealed class DatabasePayrollUnionFeeDeductionReadService(ApplicationDbContext dbContext)
    : IPayrollUnionFeeDeductionReadService
{
    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;
    private const int MaximumPageSize = 200;

    public async Task<PayrollUnionFeeDeductionPageDto> SearchAsync(
        PayrollUnionFeeDeductionFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedFilter = NormalizeFilter(filter);
        var query = BuildQuery(normalizedFilter);
        var totalCount = await query.CountAsync(cancellationToken);
        var page = await query
            .Skip(normalizedFilter.Skip)
            .Take(normalizedFilter.Take)
            .ToListAsync(cancellationToken);

        return new PayrollUnionFeeDeductionPageDto(
            page.Select(MapToDto).ToArray(),
            totalCount);
    }

    private IQueryable<PayrollUnionFeeDeductionProjection> BuildQuery(PayrollUnionFeeDeductionNormalizedFilter filter)
    {
        var payrollMonth = filter.PayrollMonth;
        var payrollYear = filter.PayrollYear;
        var searchPattern = string.IsNullOrWhiteSpace(filter.SearchText)
            ? null
            : $"%{filter.SearchText}%";

        return
            from summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
            join detail in dbContext.PayrollDeductionUnionFeeRecords.AsNoTracking()
                on summary.Id equals detail.PayrollDeductionSummaryRecordId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            where (summary.PayrollYear > MinimumSupportedYear
                || (summary.PayrollYear == MinimumSupportedYear
                    && summary.PayrollMonth >= MinimumSupportedMonth))
                && (!payrollMonth.HasValue || summary.PayrollMonth == payrollMonth.Value)
                && (!payrollYear.HasValue || summary.PayrollYear == payrollYear.Value)
                && (searchPattern == null
                    || (employee != null && employee.EmployeeCode != null && EF.Functions.ILike(employee.EmployeeCode, searchPattern))
                    || (employee != null && employee.FirstName != null && EF.Functions.ILike(employee.FirstName, searchPattern))
                    || (employee != null && employee.LastName != null && EF.Functions.ILike(employee.LastName, searchPattern))
                    || (department != null && department.DepartmentOrWorkshopName != null && EF.Functions.ILike(department.DepartmentOrWorkshopName, searchPattern))
                    || (department != null && department.TeamName != null && EF.Functions.ILike(department.TeamName, searchPattern))
                    || (department != null && department.GroupName != null && EF.Functions.ILike(department.GroupName, searchPattern))
                    || (position != null && position.Name != null && EF.Functions.ILike(position.Name, searchPattern)))
            orderby summary.PayrollYear descending,
                summary.PayrollMonth descending,
                employee == null ? string.Empty : employee.EmployeeCode,
                employee == null ? string.Empty : employee.LastName,
                employee == null ? string.Empty : employee.FirstName,
                summary.Id
            select new PayrollUnionFeeDeductionProjection(
                summary.Id,
                summary.EmployeeId,
                employee == null ? null : employee.EmployeeCode,
                employee == null ? null : employee.FirstName,
                employee == null ? null : employee.LastName,
                department == null ? null : department.DepartmentOrWorkshopName,
                department == null ? null : department.TeamName,
                department == null ? null : department.GroupName,
                department == null ? null : department.CenterName,
                position == null ? null : position.Name,
                summary.PayrollMonth,
                summary.PayrollYear,
                summary.UnionFeeDeductionAmount,
                summary.IsLocked,
                detail != null && detail.IsLocked,
                detail == null ? summary.CreatedAtUtc : detail.CreatedAtUtc,
                detail == null ? null : detail.UpdatedAtUtc);
    }

    private static PayrollUnionFeeDeductionListItemDto MapToDto(PayrollUnionFeeDeductionProjection source) =>
        new(
            source.PayrollDeductionSummaryRecordId,
            source.EmployeeId,
            source.EmployeeCode,
            BuildEmployeeName(source.EmployeeLastName, source.EmployeeFirstName),
            FirstNotEmpty(source.DepartmentGroupName, source.DepartmentTeamName, source.DepartmentName, source.DepartmentCenterName),
            source.PositionName,
            source.PayrollMonth,
            source.PayrollYear,
            source.DeductionAmount,
            source.IsSummaryLocked,
            source.IsLocked,
            source.CreatedAtUtc,
            source.UpdatedAtUtc);

    private static PayrollUnionFeeDeductionNormalizedFilter NormalizeFilter(PayrollUnionFeeDeductionFilter filter)
    {
        if (filter.PayrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.");
        }

        if (filter.PayrollYear is < MinimumSupportedYear or > MaximumSupportedYear)
        {
            throw new InvalidOperationException($"Năm kỳ lương phải nằm trong khoảng từ {MinimumSupportedYear} đến {MaximumSupportedYear}.");
        }

        if(filter.PayrollYear == MinimumSupportedYear && filter.PayrollMonth < MinimumSupportedMonth)
        {
            throw new InvalidOperationException($"Kỳ lương phải từ {MinimumSupportedMonth:00}/{MinimumSupportedYear} trở đi.");
        }

        return new PayrollUnionFeeDeductionNormalizedFilter(
            filter.PayrollMonth is null ? null : (short)filter.PayrollMonth.Value,
            filter.PayrollYear is null ? null : (short)filter.PayrollYear.Value,
            NormalizeOptional(filter.SearchText),
            Math.Max(filter.Skip, 0),
            Math.Clamp(filter.Take, 1, MaximumPageSize));
    }

    private static string? BuildEmployeeName(string? lastName, string? firstName)
    {
        var parts = new[] { lastName, firstName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

        var employeeName = string.Join(" ", parts);
        return string.IsNullOrWhiteSpace(employeeName) ? null : employeeName;
    }

    private static string? FirstNotEmpty(params string?[] values) =>
        values.Select(NormalizeOptional).FirstOrDefault(value => value is not null);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record PayrollUnionFeeDeductionNormalizedFilter(
        short? PayrollMonth,
        short? PayrollYear,
        string? SearchText,
        int Skip,
        int Take);

    private sealed record PayrollUnionFeeDeductionProjection(
        Guid PayrollDeductionSummaryRecordId,
        Guid EmployeeId,
        string? EmployeeCode,
        string? EmployeeFirstName,
        string? EmployeeLastName,
        string? DepartmentName,
        string? DepartmentTeamName,
        string? DepartmentGroupName,
        string? DepartmentCenterName,
        string? PositionName,
        short PayrollMonth,
        short PayrollYear,
        decimal DeductionAmount,
        bool IsSummaryLocked,
        bool IsLocked,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
