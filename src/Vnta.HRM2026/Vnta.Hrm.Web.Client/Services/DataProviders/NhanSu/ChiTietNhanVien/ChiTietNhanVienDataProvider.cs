using Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Models.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Services.Api.NhanSu.ChiTietNhanVien;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.NhanSu.ChiTietNhanVien;

public sealed class ChiTietNhanVienDataProvider(IChiTietNhanVienApiService apiService)
{
    public Task<EmployeeContactProfileDto?> GetContactProfileAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        apiService.GetContactProfileAsync(employeeId, cancellationToken);

    public Task<EmployeeContactProfileDto> UpsertContactProfileAsync(UpsertEmployeeContactProfileRequest request, CancellationToken cancellationToken = default) =>
        apiService.UpsertContactProfileAsync(request, cancellationToken);

    public Task<CitizenIdentityDto?> GetCitizenIdentityAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        apiService.GetCitizenIdentityAsync(employeeId, cancellationToken);

    public Task<CitizenIdentityDto> UpsertCitizenIdentityAsync(UpsertCitizenIdentityRequest request, CancellationToken cancellationToken = default) =>
        apiService.UpsertCitizenIdentityAsync(request, cancellationToken);

    public async Task<IReadOnlyList<ChiTietNhanVienRecord>> SearchAsync(
        ChiTietNhanVienFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await apiService.SearchAsync(filter, cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public async Task<ChiTietNhanVienRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await apiService.GetByIdAsync(id, cancellationToken);
        return row is null ? null : MapRecord(row);
    }

    public async Task<ChiTietNhanVienRecord> CreateAsync(
        ChiTietNhanVienEditModel model,
        CancellationToken cancellationToken = default)
    {
        var row = await apiService.CreateAsync(
            new CreateChiTietNhanVienRequest(
                model.EmployeeCode ?? string.Empty,
                model.FullName ?? string.Empty,
                model.DepartmentId ?? Guid.Empty,
                model.PositionId ?? Guid.Empty,
                (int)model.Status,
                model.HireDate),
            cancellationToken);

        return MapRecord(row);
    }

    public async Task<ChiTietNhanVienRecord> UpdateAsync(
        Guid id,
        ChiTietNhanVienEditModel model,
        CancellationToken cancellationToken = default)
    {
        var row = await apiService.UpdateAsync(
            new UpdateChiTietNhanVienRequest(
                id,
                model.EmployeeCode ?? string.Empty,
                model.FullName ?? string.Empty,
                model.DepartmentId ?? Guid.Empty,
                model.PositionId ?? Guid.Empty,
                (int)model.Status,
                model.HireDate,
                model.OriginalUpdatedAtUtc,
                model.SeniorityStartDate,
                model.ResignedDate),
            cancellationToken);

        return MapRecord(row);
    }

    public Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        apiService.DeleteAsync(ids, cancellationToken);

    private static ChiTietNhanVienRecord MapRecord(ChiTietNhanVienDto source) =>
        new()
        {
            Id = source.Id,
            EmployeeCode = source.EmployeeCode,
            FirstName = source.FirstName,
            LastName = source.LastName,
            Email = source.Email,
            PhoneNumber = source.PhoneNumber,
            AvatarDataUrl = source.AvatarDataUrl,
            HireDate = source.HireDate,
            DepartmentId = source.DepartmentId,
            DepartmentCode = source.DepartmentCode,
            DepartmentName = source.DepartmentName,
            DepartmentPath = source.DepartmentPath,
            PositionId = source.PositionId,
            PositionCode = source.PositionCode,
            PositionName = source.PositionName,
            Status = source.Status,
            SeniorityStartDate = source.SeniorityStartDate,
            ResignedDate = source.ResignedDate,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
