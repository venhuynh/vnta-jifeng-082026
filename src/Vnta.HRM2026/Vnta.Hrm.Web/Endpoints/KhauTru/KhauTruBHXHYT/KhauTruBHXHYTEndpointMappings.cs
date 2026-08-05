namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruBHXHYT;

public static class KhauTruBHXHYTEndpointMappings
{
    public static IEndpointRouteBuilder MapKhauTruBHXHYTEndpoints(this IEndpointRouteBuilder endpoints)
    {
        KhauTruBHXHYTQueryEndpoints.Map(endpoints);
        KhauTruBHXHYTCommandEndpoints.Map(endpoints);
        return endpoints;
    }
}
