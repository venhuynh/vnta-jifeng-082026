using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Sections;

/// <summary>Thanh công cụ thuần trình bày của màn hình tiến độ triển khai.</summary>
public partial class TienDoTrienKhaiToolbar
{
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public EventCallback AddRequested { get; set; }
    [Parameter] public EventCallback ResetRequested { get; set; }
    [Parameter] public EventCallback ColumnChooserRequested { get; set; }
}
