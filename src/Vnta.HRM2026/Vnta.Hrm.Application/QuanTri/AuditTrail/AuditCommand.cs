namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Server-created audit context for one logical command.
/// </summary>
public sealed record AuditCommand(
    Guid OperationId,
    string ActionIntent,
    AuditActor Actor,
    string CorrelationId,
    AuditCaptureMode CaptureMode = AuditCaptureMode.EntityChanges,
    string? EventKey = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
