using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

namespace Vnta.Hrm.Web.Client.Services.Api.NhanSu.ChiTietNhanVien;

public sealed class HttpChiTietNhanVienApiService(NavigationManager navigationManager)
    : IChiTietNhanVienApiService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<ChiTietNhanVienDto>> SearchAsync(
        ChiTietNhanVienFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/nhan-su/chi-tiet-nhan-vien/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<ChiTietNhanVienDto>>(cancellationToken);
    }

    public async Task<ChiTietNhanVienDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/nhan-su/chi-tiet-nhan-vien/{id}",
            cancellationToken);

        if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        return await response.ReadRequiredFromJsonAsync<ChiTietNhanVienDto>(cancellationToken);
    }

    public async Task<EmployeeContactProfileDto?> GetContactProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/nhan-su/chi-tiet-nhan-vien/{employeeId}/contact-profile", cancellationToken);
        return response.StatusCode == System.Net.HttpStatusCode.NotFound ? null : await response.ReadRequiredFromJsonAsync<EmployeeContactProfileDto>(cancellationToken);
    }

    public async Task<EmployeeContactProfileDto> UpsertContactProfileAsync(UpsertEmployeeContactProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/nhan-su/chi-tiet-nhan-vien/{request.EmployeeId}/contact-profile", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<EmployeeContactProfileDto>(cancellationToken);
    }

    public async Task<CitizenIdentityDto?> GetCitizenIdentityAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/nhan-su/chi-tiet-nhan-vien/{employeeId}/citizen-identity", cancellationToken);
        return response.StatusCode == System.Net.HttpStatusCode.NotFound ? null : await response.ReadRequiredFromJsonAsync<CitizenIdentityDto>(cancellationToken);
    }

    public async Task<CitizenIdentityDto> UpsertCitizenIdentityAsync(UpsertCitizenIdentityRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/nhan-su/chi-tiet-nhan-vien/{request.EmployeeId}/citizen-identity", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<CitizenIdentityDto>(cancellationToken);
    }

    public async Task<ChiTietNhanVienDto> CreateAsync(
        CreateChiTietNhanVienRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/nhan-su/chi-tiet-nhan-vien",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<ChiTietNhanVienDto>(cancellationToken);
    }

    public async Task<ChiTietNhanVienDto> UpdateAsync(
        UpdateChiTietNhanVienRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/nhan-su/chi-tiet-nhan-vien/{request.Id}",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<ChiTietNhanVienDto>(cancellationToken);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/nhan-su/chi-tiet-nhan-vien/delete",
            ids,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }
}
