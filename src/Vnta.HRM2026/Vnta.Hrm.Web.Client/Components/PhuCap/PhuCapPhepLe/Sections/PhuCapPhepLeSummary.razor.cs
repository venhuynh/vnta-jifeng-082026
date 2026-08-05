using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLeSummary
{
    [Parameter] public int AllRecordCount { get; set; }
        [Parameter] public int OpenRecordCount { get; set; }
        [Parameter] public int LockedRecordCount { get; set; }
        [Parameter] public string TotalAmountDisplay { get; set; } = string.Empty;
        [Parameter] public string? SearchText { get; set; }
        [Parameter] public bool CanInteract { get; set; }
        [Parameter] public bool CanSearch { get; set; }
        [Parameter] public bool IsAllSelected { get; set; }
        [Parameter] public bool IsOpenSelected { get; set; }
        [Parameter] public bool IsLockedSelected { get; set; }
        [Parameter] public EventCallback AllRequested { get; set; }
        [Parameter] public EventCallback OpenRequested { get; set; }
        [Parameter] public EventCallback LockedRequested { get; set; }
        [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
}
