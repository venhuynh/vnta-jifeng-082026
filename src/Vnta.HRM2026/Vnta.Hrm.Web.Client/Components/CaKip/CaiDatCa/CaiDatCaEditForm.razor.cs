using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Components.CaKip.CaiDatCa;

public partial class CaiDatCaEditForm
{
    private static readonly IReadOnlyList<ShiftStatusOption> StatusOptions =
    [
        new(1, "Đang sử dụng"),
        new(0, "Ngưng sử dụng")
    ];

    [Parameter] public AttendanceShiftRecord? Model { get; set; }

    [Parameter] public GridEditFormTemplateContext? EditFormContext { get; set; }

    [Parameter] public string? ErrorMessage { get; set; }

    [Parameter] public bool IsCreatingNewShift { get; set; }

    private string FormGroupCaption => IsCreatingNewShift
        ? "Thông tin khởi tạo"
        : "Thông tin điều chỉnh";

    private static string BuildColorSwatchCssVariable(string? colorHex)
    {
        var color = TryNormalizeHexColor(colorHex, out var normalizedColor) ? normalizedColor : "#9CA3AF";
        return $"--shift-color-swatch: {color};";
    }

    private static string GetColorInputValue(string? colorHex) =>
        TryNormalizeHexColor(colorHex, out var normalizedColor) ? normalizedColor : "#2563EB";

    private static bool TryNormalizeHexColor(string? value, out string color)
    {
        color = string.Empty;
        if(string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if(candidate.Length != 7 || candidate[0] != '#' || !candidate[1..].All(Uri.IsHexDigit))
        {
            return false;
        }

        color = candidate.ToUpperInvariant();
        return true;
    }

    private void OnColorInputChanged(ChangeEventArgs args)
    {
        if (Model is null)
        {
            return;
        }

        Model.ColorHex = args.Value?.ToString();
    }

    private sealed record ShiftStatusOption(int Value, string Text);
}
