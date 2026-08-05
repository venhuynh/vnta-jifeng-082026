using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruThueTNCN;

internal static class KhauTruThueTNCNQueryEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/personal-income-tax-deductions/search", SearchAsync);

    private static async Task<IResult> SearchAsync(
        [FromBody] PayrollPersonalIncomeTaxDeductionFilter? filter,
        [FromServices] IPayrollPersonalIncomeTaxDeductionReadService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.SearchAsync(
            filter ?? new PayrollPersonalIncomeTaxDeductionFilter(null, null, null),
            cancellationToken));
}
