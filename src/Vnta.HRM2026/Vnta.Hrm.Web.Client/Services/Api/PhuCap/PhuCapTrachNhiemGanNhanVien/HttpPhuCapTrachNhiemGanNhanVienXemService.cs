using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Typed HTTP client cho use case Xem của màn gán phụ cấp trách nhiệm theo nhân viên.</summary>
public sealed class HttpPhuCapTrachNhiemGanNhanVienXemService(NavigationManager navigationManager)
    : IPhuCapTrachNhiemGanNhanVienXemService
{
    private readonly HttpClient httpClient = new() { BaseAddress = new Uri(navigationManager.BaseUri) };

    public async Task<XemPhuCapTrachNhiemGanNhanVienResult> ExecuteAsync(
        XemPhuCapTrachNhiemGanNhanVienRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/view",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<XemPhuCapTrachNhiemGanNhanVienResult>(cancellationToken);
    }
}
