using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class BasicSalaryDataProvider(IBasicSalaryService basicSalaryService)
{
    public Task<IReadOnlyList<BasicSalaryRecord>> GetAsync(CancellationToken cancellationToken = default) =>
        GetFromServiceAsync(cancellationToken);

    public async Task<IReadOnlyList<BasicSalaryRecord>> SearchAsync(
        BasicSalaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await basicSalaryService.SearchAsync(filter, cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public Task<string?> ValidateAsync(
        BasicSalaryRecord salaryRecord,
        CancellationToken cancellationToken = default) =>
        basicSalaryService.ValidateAsync(MapRequest(salaryRecord), cancellationToken);

    public async Task SaveAsync(
        BasicSalaryRecord salaryRecord,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        await basicSalaryService.SaveAsync(MapRequest(salaryRecord), isNew, cancellationToken);
    }

    public async Task DeleteAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idSet = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        await basicSalaryService.DeleteAsync(idSet, cancellationToken);
    }

    public Task<SyncBasicSalaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncBasicSalaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default) =>
        basicSalaryService.SyncFromPreviousMonthAsync(request, cancellationToken);

    private async Task<IReadOnlyList<BasicSalaryRecord>> GetFromServiceAsync(CancellationToken cancellationToken)
    {
        return await SearchAsync(new BasicSalaryFilter(null), cancellationToken);
    }

    private static UpsertBasicSalaryRecordRequest MapRequest(BasicSalaryRecord source) =>
        new()
        {
            Id = source.Id,
            EmployeeId = source.EmployeeId ?? Guid.Empty,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            BasicSalary = source.BasicSalary,
            StandardWorkingDays = source.StandardWorkingDays,
            DailySalary = source.DailySalary,
            HourlySalary = source.HourlySalary,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static BasicSalaryRecord MapRecord(BasicSalaryListItemDto source) =>
        new()
        {
            Id = source.Id,
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            DepartmentPath = source.DepartmentPath,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            BasicSalary = source.BasicSalary,
            StandardWorkingDays = source.StandardWorkingDays,
            DailySalary = source.DailySalary,
            HourlySalary = source.HourlySalary,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
