namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruThueTNCN;

public static class KhauTruThueTNCNEndpointMappings
{
    public static IEndpointRouteBuilder MapKhauTruThueTNCNEndpoints(this IEndpointRouteBuilder endpoints)
    {
        KhauTruThueTNCNQueryEndpoints.Map(endpoints);
        KhauTruThueTNCNCommandEndpoints.Map(endpoints);
        return endpoints;
    }
}
