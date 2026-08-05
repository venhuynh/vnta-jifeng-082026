using System.Text.Json.Serialization;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

public sealed record UpdateOtherAllowanceRequest(
    Guid Id,
    string AllowanceName,
    bool IsFixedAmount,
    decimal AllowanceAmount,
    string? Note,
    DateTime? OriginalUpdatedAtUtc,
    [property: JsonIgnore] string RequestedBy = "");
