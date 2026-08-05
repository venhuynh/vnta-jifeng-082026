using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLeLoadError
{
    [Parameter] public string? Message { get; set; }
        [Parameter] public EventCallback RetryRequested { get; set; }
}
