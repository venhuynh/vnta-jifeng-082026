using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

public partial class PhuCapTrachNhiemPerformanceBonusPopup
{
    private static readonly IReadOnlyList<(string Text, decimal Value)> PerformanceBonusOptions =
    [
        ("Thưởng 115%", 1.15m),
        ("Thưởng 100%", 1m),
        ("Thưởng 95%", 0.95m),
        ("Thưởng 90%", 0.9m),
        ("Thưởng 85%", 0.85m),
        ("Thưởng 80%", 0.8m)
    ];

    private bool wasVisible;
    private decimal? SelectedRate { get; set; }
    private decimal? CustomPercentage { get; set; }
    private string? LocalErrorMessage { get; set; }
    private decimal? ResolvedRate => CustomPercentage.HasValue ? CustomPercentage.Value / 100m : SelectedRate;
    private string ResolvedRateText => ResolvedRate is { } rate ? $"{rate:P2}" : "Chưa chọn";

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string PeriodLabel { get; set; } = string.Empty;
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback<decimal> SaveRequested { get; set; }

    protected override void OnParametersSet()
    {
        if (Visible && !wasVisible)
        {
            SelectedRate = null;
            CustomPercentage = null;
            LocalErrorMessage = null;
        }

        wasVisible = Visible;
    }

    private bool IsPredefinedRateSelected(decimal value) => SelectedRate == value && !CustomPercentage.HasValue;

    private string GetOptionCssClass(decimal value) =>
        IsPredefinedRateSelected(value) ? "performance-bonus-option is-selected" : "performance-bonus-option";

    private void SelectRate(decimal value)
    {
        SelectedRate = value;
        CustomPercentage = null;
        LocalErrorMessage = null;
    }

    private Task OnCustomPercentageChanged(decimal? value)
    {
        CustomPercentage = value;
        if (value.HasValue)
        {
            SelectedRate = null;
        }

        LocalErrorMessage = null;
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        var resolvedRate = ResolvedRate;
        if (!resolvedRate.HasValue)
        {
            LocalErrorMessage = "Vui lòng chọn mức thưởng hoặc nhập hệ số tự do.";
            return;
        }

        if (resolvedRate.Value < 0m)
        {
            LocalErrorMessage = "Hệ số thưởng hiệu suất không được âm.";
            return;
        }

        LocalErrorMessage = null;
        await SaveRequested.InvokeAsync(decimal.Round(resolvedRate.Value, 4, MidpointRounding.AwayFromZero));
    }
}
