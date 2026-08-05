namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeListItemDto>> SearchAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default);

    Task<EmployeeSummaryDto> GetSummaryAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default);

    Task<EmployeeListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeListItemDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<EmployeeListItemDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeListItemDto> UpdateAsync(UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
