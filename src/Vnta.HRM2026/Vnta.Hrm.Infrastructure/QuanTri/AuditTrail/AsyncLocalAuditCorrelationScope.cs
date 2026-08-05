using System.Threading;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// Holds a correlation identifier in the current logical async flow. Singleton registration is
/// safe because the value is stored in <see cref="AsyncLocal{T}"/>.
/// </summary>
public sealed class AsyncLocalAuditCorrelationScope : IAuditCorrelationScope
{
    private readonly AsyncLocal<Frame?> _current = new();

    public string? Current => _current.Value?.CorrelationId;

    public IDisposable Begin(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128)
        {
            throw new ArgumentException(
                "An audit correlation id between 1 and 128 characters is required.",
                nameof(correlationId));
        }

        var frame = new Frame(correlationId, _current.Value);
        _current.Value = frame;
        return new ScopeLease(this, frame);
    }

    private void End(Frame frame)
    {
        if (!ReferenceEquals(_current.Value, frame))
        {
            throw new InvalidOperationException(
                "Audit correlation scopes must be disposed in reverse order on the same async flow.");
        }

        _current.Value = frame.Parent;
    }

    private sealed class Frame(string correlationId, Frame? parent)
    {
        public string CorrelationId { get; } = correlationId;

        public Frame? Parent { get; } = parent;
    }

    private sealed class ScopeLease(AsyncLocalAuditCorrelationScope owner, Frame frame) : IDisposable
    {
        private AsyncLocalAuditCorrelationScope? _owner = owner;
        private Frame? _frame = frame;

        public void Dispose()
        {
            var owner = _owner;
            var frame = _frame;
            if (owner is null || frame is null)
            {
                return;
            }

            owner.End(frame);
            _owner = null;
            _frame = null;
        }
    }
}
