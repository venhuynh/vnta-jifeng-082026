using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Popup điều chỉnh thủ công phụ cấp cơm.</summary>
public partial class PhuCapComEditPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Điều chỉnh phụ cấp cơm";
    [Parameter] public PhuCapComEditModel Model { get; set; } = new();
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public string? ValidationMessage { get; set; }
    [Parameter] public EventCallback<int> QualifiedMealDaysChanged { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);

    private Task SaveAsync() => SaveRequested.InvokeAsync();

    private Task OnQualifiedMealDaysChangedAsync(int value) =>
        QualifiedMealDaysChanged.InvokeAsync(Math.Max(0, value));

    private static string FormatNumber(int value) => value.ToString("N0", DisplayCulture);

    private static string FormatMoney(decimal value) => $"{value.ToString("N0", DisplayCulture)} đ";
}
