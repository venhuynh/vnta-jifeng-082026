using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLeExportGrid
{
    private IGrid? grid;

        [Parameter, EditorRequired] public IReadOnlyList<LeaveHolidayAllowanceRecord> Records { get; set; } = [];

        public Task ExportToExcelAsync() =>
            grid?.ExportToXlsxAsync("leave-holiday-allowances") ?? Task.CompletedTask;

        public Task ExportToPdfAsync() =>
            grid?.ExportToPdfAsync("leave-holiday-allowances") ?? Task.CompletedTask;
}
