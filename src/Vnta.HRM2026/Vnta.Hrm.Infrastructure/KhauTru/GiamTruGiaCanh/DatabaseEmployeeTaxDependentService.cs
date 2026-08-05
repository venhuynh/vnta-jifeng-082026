using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.KhauTru.GiamTruGiaCanh;

public sealed class DatabaseEmployeeTaxDependentService(ApplicationDbContext dbContext) : IEmployeeTaxDependentService
{
    private const int MaximumPageSize = 200;

    public async Task<IReadOnlyList<EmployeeTaxDependentDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty) throw new InvalidOperationException("Thiếu định danh nhân viên.");
        return await dbContext.PayrollEmployeeTaxDependents.AsNoTracking().Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.DependentFullName).Select(x => Map(x)).ToListAsync(cancellationToken);
    }

    public async Task<EmployeeTaxDependentPageDto> SearchAsync(
        EmployeeTaxDependentFilter filter,
        CancellationToken cancellationToken = default)
    {
        var searchText = Normalize(filter.SearchText);
        var query =
            from dependent in dbContext.PayrollEmployeeTaxDependents.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on dependent.EmployeeId equals employee.Id
            select new { dependent, employee };

        if (filter.IsFamilyDeductionRegistered.HasValue)
        {
            query = query.Where(item => item.dependent.IsFamilyDeductionRegistered == filter.IsFamilyDeductionRegistered.Value);
        }

        if (searchText is not null)
        {
            var pattern = $"%{searchText}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.dependent.DependentFullName, pattern)
                || EF.Functions.ILike(item.employee.EmployeeCode, pattern)
                || EF.Functions.ILike(item.employee.FirstName, pattern)
                || EF.Functions.ILike(item.employee.LastName, pattern)
                || (item.dependent.DependentTaxCode != null && EF.Functions.ILike(item.dependent.DependentTaxCode, pattern))
                || (item.dependent.DependentIdentityNumber != null && EF.Functions.ILike(item.dependent.DependentIdentityNumber, pattern))
                || (item.dependent.RelationshipToEmployee != null && EF.Functions.ILike(item.dependent.RelationshipToEmployee, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(item => item.employee.EmployeeCode)
            .ThenBy(item => item.employee.LastName)
            .ThenBy(item => item.employee.FirstName)
            .ThenBy(item => item.dependent.DependentFullName)
            .ThenBy(item => item.dependent.Id)
            .Skip(Math.Max(filter.Skip, 0))
            .Take(Math.Clamp(filter.Take, 1, MaximumPageSize))
            .Select(item => new EmployeeTaxDependentSearchRow(
                item.dependent,
                item.employee.EmployeeCode,
                item.employee.LastName,
                item.employee.FirstName))
            .ToListAsync(cancellationToken);

        return new EmployeeTaxDependentPageDto(
            rows.Select(row => new EmployeeTaxDependentListItemDto(
                Map(row.Dependent),
                row.EmployeeCode,
                BuildEmployeeName(row.EmployeeLastName, row.EmployeeFirstName)))
                .ToArray(),
            totalCount);
    }

    public async Task<EmployeeTaxDependentDto> SaveAsync(SaveEmployeeTaxDependentRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var employeeExists = await dbContext.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (!employeeExists) throw new InvalidOperationException("Không tìm thấy nhân viên của hồ sơ người phụ thuộc.");
        var row = request.Id == Guid.Empty ? null : await dbContext.PayrollEmployeeTaxDependents.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        var isNew = row is null;
        if (request.Id != Guid.Empty && row is null) throw new InvalidOperationException("Không tìm thấy hồ sơ người phụ thuộc.");
        if (row?.EmployeeId != null && row.EmployeeId != request.EmployeeId) throw new InvalidOperationException("Không thể chuyển hồ sơ người phụ thuộc sang nhân viên khác.");
        if (row is not null)
        {
            var currentConcurrencyToken = row.UpdatedAtUtc ?? row.CreatedAtUtc;
            if (!request.OriginalUpdatedAtUtc.HasValue
                || request.OriginalUpdatedAtUtc.Value != currentConcurrencyToken)
                throw new DbUpdateConcurrencyException("Hồ sơ người phụ thuộc đã được cập nhật bởi người dùng khác.");
        }
        var from = ToMonth(request.DeductionFromMonth); var to = ToMonth(request.DeductionToMonth);
        var hasLockedExistingPeriod = row is not null
            && await HasLockedAffectedPeriodAsync(row.EmployeeId, row.DeductionFromMonth, row.DeductionToMonth, cancellationToken);
        if (hasLockedExistingPeriod || await HasLockedAffectedPeriodAsync(request.EmployeeId, from, to, cancellationToken))
            throw new InvalidOperationException("Không thể sửa hồ sơ người phụ thuộc vì có kỳ lương hoặc snapshot giảm trừ gia cảnh đã khóa trong khoảng hiệu lực.");
        var actor = Actor(request.Actor); var now = Now();
        if (row is null)
        {
            row = new PayrollEmployeeTaxDependentRow { EmployeeId = request.EmployeeId, CreatedBy = actor };
            dbContext.PayrollEmployeeTaxDependents.Add(row);
        }

        row.EmployeeTaxCode = Text(request.EmployeeTaxCode, 4000);
        row.RegistrationDate = request.RegistrationDate;
        row.DependentFullName = request.DependentFullName.Trim();
        row.DependentGender = Text(request.DependentGender, 4000);
        row.DependentBirthDate = request.DependentBirthDate;
        row.DependentIdentityNumber = Text(request.DependentIdentityNumber, 4000);
        row.DependentTaxCode = Text(request.DependentTaxCode, 4000);
        row.DependentNationality = Text(request.DependentNationality, 4000);
        row.EmployeeIdentityNumber = Text(request.EmployeeIdentityNumber, 4000);
        row.RelationshipToEmployee = Text(request.RelationshipToEmployee, 4000);
        row.IsFamilyDeductionRegistered = request.IsFamilyDeductionRegistered;
        row.RegistrationBookNumber = Text(request.RegistrationBookNumber, 128);
        row.RegistrationPageNumber = Text(request.RegistrationPageNumber, 4000);
        if (isNew)
        {
            row.CountryName = Text(request.CountryName, 4000);
            row.OldWardCode = Text(request.OldWardCode, 4000);
            row.OldWardName = Text(request.OldWardName, 4000);
            row.OldDistrictCode = Text(request.OldDistrictCode, 4000);
            row.OldDistrictName = Text(request.OldDistrictName, 4000);
            row.OldProvinceCode = Text(request.OldProvinceCode, 4000);
            row.OldProvinceName = Text(request.OldProvinceName, 4000);
            row.NewWardCode = Text(request.NewWardCode, 4000);
            row.NewWardName = Text(request.NewWardName, 4000);
            row.NewDistrictCode = Text(request.NewDistrictCode, 4000);
            row.NewDistrictName = Text(request.NewDistrictName, 4000);
            row.NewProvinceCode = Text(request.NewProvinceCode, 4000);
            row.NewProvinceName = Text(request.NewProvinceName, 4000);
        }
        row.DeductionFromMonth = from;
        row.DeductionToMonth = to;
        row.GhiChu = Text(request.GhiChu, 4000);
        row.UpdatedAtUtc = now; row.UpdatedBy = actor;
        await dbContext.SaveChangesAsync(cancellationToken); return Map(row);
    }

    private async Task<bool> HasLockedAffectedPeriodAsync(Guid employeeId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var summaries = dbContext.PayrollDeductionSummaryRecords.Where(x => x.EmployeeId == employeeId && x.IsLocked);
        return await summaries.AnyAsync(x => (!from.HasValue || x.PayrollYear > from.Value.Year || (x.PayrollYear == from.Value.Year && x.PayrollMonth >= from.Value.Month)) && (!to.HasValue || x.PayrollYear < to.Value.Year || (x.PayrollYear == to.Value.Year && x.PayrollMonth <= to.Value.Month)), ct);
    }
    private static void Validate(SaveEmployeeTaxDependentRequest x)
    {
        if (x.EmployeeId == Guid.Empty || string.IsNullOrWhiteSpace(x.DependentFullName)) throw new InvalidOperationException("Nhân viên và tên người phụ thuộc là bắt buộc.");
        if (x.DependentFullName.Trim().Length > 4000) throw new InvalidOperationException("Tên người phụ thuộc không được dài quá 4.000 ký tự.");
        var from = ToMonth(x.DeductionFromMonth);
        var to = ToMonth(x.DeductionToMonth);
        if (from.HasValue && to.HasValue && to.Value < from.Value) throw new InvalidOperationException("Tháng kết thúc giảm trừ không được trước tháng bắt đầu.");
    }
    private static DateOnly? ToMonth(DateOnly? value) => value is { } x ? new DateOnly(x.Year, x.Month, 1) : null;
    private static string? Text(string? value, int maximum) { var result = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); if (result?.Length > maximum) throw new InvalidOperationException($"Dữ liệu không được dài quá {maximum} ký tự."); return result; }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? BuildEmployeeName(string? lastName, string? firstName)
    {
        var value = string.Join(" ", new[] { lastName, firstName }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
    private static string Actor(string? value) => Text(value, 128) ?? "system";
    private static DateTime Now() => DateTime.UtcNow;
    private static EmployeeTaxDependentDto Map(PayrollEmployeeTaxDependentRow x) => new(
        x.Id, x.EmployeeId, x.EmployeeTaxCode, x.RegistrationDate, x.DependentFullName, x.DependentGender,
        x.DependentBirthDate, x.DependentIdentityNumber, x.DependentTaxCode, x.DependentNationality,
        x.EmployeeIdentityNumber, x.RelationshipToEmployee, x.IsFamilyDeductionRegistered, x.RegistrationBookNumber,
        x.RegistrationPageNumber, x.CountryName, x.OldWardCode, x.OldWardName, x.OldDistrictCode,
        x.OldDistrictName, x.OldProvinceCode, x.OldProvinceName, x.NewWardCode, x.NewWardName,
        x.NewDistrictCode, x.NewDistrictName, x.NewProvinceCode, x.NewProvinceName, x.DeductionFromMonth,
        x.DeductionToMonth, x.GhiChu, x.CreatedAtUtc, x.UpdatedAtUtc);

    private sealed record EmployeeTaxDependentSearchRow(
        PayrollEmployeeTaxDependentRow Dependent,
        string EmployeeCode,
        string EmployeeLastName,
        string EmployeeFirstName);
}
