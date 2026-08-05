using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLeEditForm
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

        [Parameter]
        public LeaveHolidayManualEditModel? Model { get; set; }

        [Parameter]
        public EditContext? EditContext { get; set; }

        [Parameter]
        public string? ErrorMessage { get; set; }

        [Parameter]
        public bool IsSaving { get; set; }

        private static string FormatMoney(decimal value) =>
            value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);

        private static string FormatQuantity(decimal value) =>
            string.Format(DisplayCulture, "{0:N2}", value);
}
