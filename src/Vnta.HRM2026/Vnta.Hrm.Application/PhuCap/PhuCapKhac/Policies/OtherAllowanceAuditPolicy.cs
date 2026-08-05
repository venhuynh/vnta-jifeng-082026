namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;

/// <summary>Normalizes the actor persisted with an other-allowance audit trail.</summary>
public static class OtherAllowanceAuditPolicy
{
    public static OtherAllowanceAuditActor ResolveActor(string? requestedBy) =>
        new(OtherAllowanceDefinitionPolicy.NormalizeOptionalText(requestedBy) ?? "system");
}

public sealed record OtherAllowanceAuditActor(string Value);
