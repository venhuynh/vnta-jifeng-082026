using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.SharedUi.Progress;

public partial class VntaCircularProgressCallout : IDisposable
{
    private const double InitialAnimatedValue = 12;
    private CancellationTokenSource? animationTokenSource;
    private Task? animationTask;
    private double animatedValue = InitialAnimatedValue;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public double? Value { get; set; }
    [Parameter] public bool AutoAnimate { get; set; } = true;
    [Parameter] public bool ShowLabel { get; set; } = true;
    [Parameter] public bool Compact { get; set; }
    [Parameter] public string Size { get; set; } = "84px";
    [Parameter] public string AriaLabel { get; set; } = "Tiến trình đang chạy";
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Message { get; set; }
    [Parameter] public string? Detail { get; set; }

    private bool ShouldAnimate => Visible && AutoAnimate && !Value.HasValue;

    private double ResolvedValue => Clamp(Value ?? animatedValue);

    private string RootCssClass => Compact
        ? "vnta-circular-progress-callout vnta-circular-progress-callout--compact"
        : "vnta-circular-progress-callout";

    protected override void OnParametersSet()
    {
        if (ShouldAnimate)
        {
            EnsureAnimation();
            return;
        }

        StopAnimation();
        animatedValue = Visible && Value.HasValue
            ? Clamp(Value.Value)
            : InitialAnimatedValue;
    }

    private void EnsureAnimation()
    {
        if (animationTask is { IsCompleted: false })
        {
            return;
        }

        StopAnimation();
        animationTokenSource = new CancellationTokenSource();
        animationTask = AnimateAsync(animationTokenSource.Token);
    }

    private async Task AnimateAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(180, cancellationToken);
                animatedValue = GetNextAnimatedValue(animatedValue);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void StopAnimation()
    {
        if (animationTokenSource is null)
        {
            return;
        }

        animationTokenSource.Cancel();
        animationTokenSource.Dispose();
        animationTokenSource = null;
        animationTask = null;
    }

    private static double GetNextAnimatedValue(double currentValue) => currentValue switch
    {
        < 60 => currentValue + 8,
        < 80 => currentValue + 4,
        < 92 => currentValue + 1.5,
        _ => 76
    };

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);

    public void Dispose() => StopAnimation();
}
