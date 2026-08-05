using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruBHXHYT;

internal static class KhauTruBHXHYTQueryEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/social-health-insurance-deductions/search", SearchPayrollInsuranceDeductionsAsync);
    }

    private static async Task<IResult> SearchPayrollInsuranceDeductionsAsync(
        [FromBody] PayrollInsuranceDeductionFilter? filter,
        [FromServices] IPayrollInsuranceDeductionReadService payrollInsuranceDeductionService,
        CancellationToken cancellationToken)
    {
        var result = await payrollInsuranceDeductionService.SearchAsync(
            filter ?? new PayrollInsuranceDeductionFilter(null, null, null),
            cancellationToken);

        return Results.Ok(result);
    }
}
