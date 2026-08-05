namespace Vnta.Hrm.Web.Client.Models.Payroll;

/// <summary>
/// Kết quả page đã được map sang model UI; provider vẫn giữ DTO Application
/// phía sau boundary client.
/// </summary>
public sealed record PayrollAllowanceSummaryLoadResult(
    IReadOnlyList<PayrollAllowanceSummaryRecord> Rows,
    int TotalCount);
