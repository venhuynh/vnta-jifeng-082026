namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruPhiCongDoan;

public static class KhauTruPhiCongDoanEndpointMappings
{
    public static IEndpointRouteBuilder MapKhauTruPhiCongDoanEndpoints(this IEndpointRouteBuilder endpoints)
    {
        KhauTruPhiCongDoanQueryEndpoints.Map(endpoints);
        KhauTruPhiCongDoanCommandEndpoints.Map(endpoints);
        return endpoints;
    }
}
