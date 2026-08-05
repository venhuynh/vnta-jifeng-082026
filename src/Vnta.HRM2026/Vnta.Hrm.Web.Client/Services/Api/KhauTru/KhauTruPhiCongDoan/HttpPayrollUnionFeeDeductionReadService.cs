using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpPayrollUnionFeeDeductionReadService(NavigationManager navigationManager)
    : IPayrollUnionFeeDeductionReadService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<PayrollUnionFeeDeductionPageDto> SearchAsync(
        PayrollUnionFeeDeductionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/union-fee-deductions/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollUnionFeeDeductionPageDto>(cancellationToken);
    }
}
