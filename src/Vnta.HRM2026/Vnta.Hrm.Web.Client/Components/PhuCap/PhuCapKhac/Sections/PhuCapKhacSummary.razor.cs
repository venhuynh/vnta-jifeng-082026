using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.State;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Sections;

public partial class PhuCapKhacSummary
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter, EditorRequired] public OtherAllowanceGridState State { get; set; } = default!;
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }

    private static string FormatVnd(decimal amount) => string.Format(DisplayCulture, "{0:N0} đ", amount);
}
