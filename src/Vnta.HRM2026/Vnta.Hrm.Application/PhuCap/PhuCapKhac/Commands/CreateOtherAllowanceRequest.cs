using System.Text.Json.Serialization;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

public sealed record CreateOtherAllowanceRequest(
    Guid PayrollAllowanceSummaryRecordId,
    string AllowanceName,
    bool IsFixedAmount,
    decimal AllowanceAmount,
    string? Note,
    [property: JsonIgnore] string RequestedBy = "");
