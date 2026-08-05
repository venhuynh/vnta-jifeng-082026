namespace Vnta.Hrm.Web.Client.Services.Adms;

public sealed class AdmsGatewayMonitorOptions {
    public string BaseUrl { get; set; } = string.Empty;
    public string HubPath { get; set; } = "/hubs/adms-monitor";
    public int ActivityBufferLimit { get; set; } = 50;
    public int RawBufferLimit { get; set; } = 50;
    public int RawPanelEventLimit { get; set; } = 50;
    public int DeviceOfflineTimeoutSeconds { get; set; } = 180;
}
