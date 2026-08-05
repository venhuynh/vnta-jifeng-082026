using System.Text.Json.Serialization;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

public sealed record DeleteOtherAllowanceRequest(
    Guid Id,
    DateTime? OriginalUpdatedAtUtc,
    [property: JsonIgnore] string RequestedBy = "");
