namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

public sealed partial class OtherAllowanceCoordinator
{
    public void Initialize()
    {
        var defaultPeriod = GetDefaultPayrollPeriod();
        ToolbarMonth = defaultPeriod.Month;
        ToolbarYear = defaultPeriod.Year;
    }

    private async Task ExecuteExclusiveAsync(Func<Task> operation)
    {
        if(disposalTokenSource.IsCancellationRequested || !await commandGate.WaitAsync(0)) return;
        try { await operation(); }
        finally { commandGate.Release(); }
    }

    public void Dispose()
    {
        if(Interlocked.Exchange(ref disposed, 1) != 0) return;
        disposalTokenSource.Cancel();
        var loadCancellationSource = Interlocked.Exchange(ref loadCancellationTokenSource, null);
        loadCancellationSource?.Cancel();
        loadCancellationSource?.Dispose();
        disposalTokenSource.Dispose();
    }
}
