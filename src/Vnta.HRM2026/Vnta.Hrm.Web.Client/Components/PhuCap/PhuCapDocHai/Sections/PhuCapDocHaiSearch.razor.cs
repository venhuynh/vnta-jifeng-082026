using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Sections;

public partial class PhuCapDocHaiSearch
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public bool Enabled { get; set; }
    [Parameter] public EventCallback<string?> TextChanged { get; set; }
}
