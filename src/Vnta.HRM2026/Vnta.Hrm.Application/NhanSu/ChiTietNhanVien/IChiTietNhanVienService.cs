namespace Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

public interface IChiTietNhanVienService
{
    Task<IReadOnlyList<ChiTietNhanVienDto>> SearchAsync(
        ChiTietNhanVienFilter filter,
        CancellationToken cancellationToken = default);

    Task<ChiTietNhanVienDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EmployeeContactProfileDto?> GetContactProfileAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeeContactProfileDto> UpsertContactProfileAsync(
        UpsertEmployeeContactProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<CitizenIdentityDto?> GetCitizenIdentityAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<CitizenIdentityDto> UpsertCitizenIdentityAsync(
        UpsertCitizenIdentityRequest request,
        CancellationToken cancellationToken = default);

    Task<ChiTietNhanVienDto> CreateAsync(
        CreateChiTietNhanVienRequest request,
        CancellationToken cancellationToken = default);

    Task<ChiTietNhanVienDto> UpdateAsync(
        UpdateChiTietNhanVienRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
