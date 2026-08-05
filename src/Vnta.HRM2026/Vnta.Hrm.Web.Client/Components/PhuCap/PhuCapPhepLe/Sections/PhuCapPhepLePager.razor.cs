using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLePager
{
    [Parameter] public string SummaryText { get; set; } = string.Empty;
        [Parameter] public int PageCount { get; set; }
        [Parameter] public int ActivePageIndex { get; set; }
        [Parameter] public IReadOnlyList<int> PageSizeOptions { get; set; } = [];
        [Parameter] public int PageSize { get; set; }
        [Parameter] public bool CanBrowsePages { get; set; }
        [Parameter] public bool CanChangeFilters { get; set; }
        [Parameter] public EventCallback<int> ActivePageIndexChanged { get; set; }
        [Parameter] public EventCallback<int> PageSizeChanged { get; set; }
}
