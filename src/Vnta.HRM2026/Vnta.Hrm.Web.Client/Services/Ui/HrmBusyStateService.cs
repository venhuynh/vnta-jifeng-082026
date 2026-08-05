using System.Threading;

namespace Vnta.Hrm.Web.Client.Services.Ui;

public sealed class HrmBusyStateService
{
    private int busyCount;

    public bool IsBusy => Volatile.Read(ref busyCount) > 0;

    public event Action? Changed;

    public IDisposable Enter()
    {
        Interlocked.Increment(ref busyCount);
        Changed?.Invoke();
        return new BusyScope(this);
    }

    private void Exit()
    {
        var next = Interlocked.Decrement(ref busyCount);
        if (next < 0)
        {
            Interlocked.Exchange(ref busyCount, 0);
        }

        Changed?.Invoke();
    }

    private sealed class BusyScope(HrmBusyStateService owner) : IDisposable
    {
        private HrmBusyStateService? owner = owner;

        public void Dispose()
        {
            var currentOwner = Interlocked.Exchange(ref owner, null);
            currentOwner?.Exit();
        }
    }
}
