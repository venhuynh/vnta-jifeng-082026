using System.Text.Json.Serialization;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

/// <summary>Copies missing other-allowance lines from the preceding payroll period.</summary>
public sealed record SyncOtherAllowanceFromPreviousMonthRequest(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    [property: JsonIgnore] string RequestedBy = "");
