using Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Services.Api.NhanSu.ChiTietNhanVien;

namespace Vnta.Hrm.Web.Services.NhanSu.ChiTietNhanVien;

public sealed class ServerChiTietNhanVienApiService(IChiTietNhanVienService service)
    : IChiTietNhanVienApiService
{
    public Task<IReadOnlyList<ChiTietNhanVienDto>> SearchAsync(
        ChiTietNhanVienFilter filter,
        CancellationToken cancellationToken = default) =>
        service.SearchAsync(filter, cancellationToken);

    public Task<ChiTietNhanVienDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        service.GetByIdAsync(id, cancellationToken);

    public Task<EmployeeContactProfileDto?> GetContactProfileAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        service.GetContactProfileAsync(employeeId, cancellationToken);

    public Task<EmployeeContactProfileDto> UpsertContactProfileAsync(UpsertEmployeeContactProfileRequest request, CancellationToken cancellationToken = default) =>
        service.UpsertContactProfileAsync(request, cancellationToken);

    public Task<CitizenIdentityDto?> GetCitizenIdentityAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        service.GetCitizenIdentityAsync(employeeId, cancellationToken);

    public Task<CitizenIdentityDto> UpsertCitizenIdentityAsync(UpsertCitizenIdentityRequest request, CancellationToken cancellationToken = default) =>
        service.UpsertCitizenIdentityAsync(request, cancellationToken);

    public Task<ChiTietNhanVienDto> CreateAsync(
        CreateChiTietNhanVienRequest request,
        CancellationToken cancellationToken = default) =>
        service.CreateAsync(request, cancellationToken);

    public Task<ChiTietNhanVienDto> UpdateAsync(
        UpdateChiTietNhanVienRequest request,
        CancellationToken cancellationToken = default) =>
        service.UpdateAsync(request, cancellationToken);

    public Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        service.DeleteAsync(ids, cancellationToken);
}
