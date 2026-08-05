using System.Text.Json.Serialization;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

public sealed record SetOtherAllowanceLockStateRequest(
    Guid Id,
    bool IsLocked,
    DateTime? OriginalUpdatedAtUtc,
    [property: JsonIgnore] string RequestedBy = "");
