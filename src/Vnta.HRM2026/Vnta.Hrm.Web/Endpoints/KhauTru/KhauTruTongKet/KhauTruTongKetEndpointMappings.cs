namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruTongKet;

/// <summary>Registers the existing deduction-summary HTTP contract beneath the authorized payroll group.</summary>
public static class KhauTruTongKetEndpointMappings
{
    public static RouteGroupBuilder MapKhauTruTongKetEndpoints(this RouteGroupBuilder payrollGroup)
    {
        payrollGroup.MapKhauTruTongKetQueryEndpoints();
        payrollGroup.MapKhauTruTongKetCommandEndpoints();
        return payrollGroup;
    }
}
