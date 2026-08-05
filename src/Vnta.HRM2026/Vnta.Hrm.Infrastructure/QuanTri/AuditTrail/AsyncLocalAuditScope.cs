using System.Threading;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// Keeps an audit command in the current logical async flow. It is safe to register as a
/// singleton because the state itself is held by <see cref="AsyncLocal{T}"/>, not by a circuit
/// or DbContext instance.
/// </summary>
public sealed class AsyncLocalAuditScope : IAuditScope
{
    private readonly AsyncLocal<ScopeFrame?> _current = new();

    public AuditCommand? Current => _current.Value?.Command;

    internal AuditOperationOutcome? CurrentOutcome => _current.Value?.Outcome;

    public IDisposable Begin(AuditCommand command)
    {
        ValidateCommand(command);

        var frame = new ScopeFrame(command, _current.Value);
        _current.Value = frame;
        return new ScopeLease(this, frame);
    }

    public void RefineAction(string finalAction)
    {
        if (string.IsNullOrWhiteSpace(finalAction))
        {
            throw new ArgumentException("An audit action is required.", nameof(finalAction));
        }

        if (finalAction.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(finalAction), "An audit action cannot exceed 100 characters.");
        }

        var frame = _current.Value
            ?? throw new InvalidOperationException("No audit scope is active for the current async flow.");

        frame.Command = frame.Command with { ActionIntent = finalAction };
    }

    public void SetOperationOutcome(AuditOperationOutcome outcome)
    {
        var frame = _current.Value
            ?? throw new InvalidOperationException("No audit scope is active for the current async flow.");

        frame.Outcome = outcome;
    }

    private void End(ScopeFrame frame)
    {
        if (!ReferenceEquals(_current.Value, frame))
        {
            throw new InvalidOperationException("Audit scopes must be disposed in reverse order on the same async flow.");
        }

        _current.Value = frame.Parent;
    }

    private static void ValidateCommand(AuditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.OperationId == Guid.Empty)
        {
            throw new ArgumentException("An audit operation id is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.ActionIntent) || command.ActionIntent.Length > 100)
        {
            throw new ArgumentException("An audit action between 1 and 100 characters is required.", nameof(command));
        }

        if (command.Actor is null || string.IsNullOrWhiteSpace(command.Actor.ActorId))
        {
            throw new ArgumentException("An audit actor id is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Actor.DisplayName))
        {
            throw new ArgumentException("An audit actor display name is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.CorrelationId) || command.CorrelationId.Length > 128)
        {
            throw new ArgumentException("An audit correlation id between 1 and 128 characters is required.", nameof(command));
        }

        if (command.EventKey?.Length > 200)
        {
            throw new ArgumentException("An audit event key cannot exceed 200 characters.", nameof(command));
        }

        if (command.EventKey is not null && string.IsNullOrWhiteSpace(command.EventKey))
        {
            throw new ArgumentException("An audit event key cannot be blank.", nameof(command));
        }

        if (command.CaptureMode == AuditCaptureMode.EntityChanges && command.EventKey is not null)
        {
            throw new ArgumentException(
                "An audit event key is only valid for an operation-level command.",
                nameof(command));
        }

        if (command.CaptureMode != AuditCaptureMode.OperationOnly
            && !string.IsNullOrWhiteSpace(command.EventKey))
        {
            throw new ArgumentException(
                "An event key is only valid for an operation-level audit command.",
                nameof(command));
        }
    }

    private sealed class ScopeFrame(AuditCommand command, ScopeFrame? parent)
    {
        public AuditCommand Command { get; set; } = command;

        public ScopeFrame? Parent { get; } = parent;

        public AuditOperationOutcome? Outcome { get; set; }
    }

    private sealed class ScopeLease(AsyncLocalAuditScope owner, ScopeFrame frame) : IDisposable
    {
        private readonly object _sync = new();
        private AsyncLocalAuditScope? _owner = owner;
        private ScopeFrame? _frame = frame;

        public void Dispose()
        {
            lock (_sync)
            {
                var owner = _owner;
                var frame = _frame;
                if (owner is null || frame is null)
                {
                    return;
                }

                // Do not consume the lease until End has verified LIFO disposal. Otherwise an
                // accidental out-of-order dispose would leave the parent frame permanently set.
                owner.End(frame);
                _owner = null;
                _frame = null;
            }
        }
    }
}
