namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Characterizes the optimistic-concurrency token used by the manual adjustment use case.
/// Persistence still performs the conditional update; this policy keeps validation testable.
/// </summary>
public static class PayrollInsuranceDeductionConcurrencyPolicy
{
    public static void EnsureExpectedVersionProvided(DateTime expectedUpdatedAtUtc)
    {
        if (expectedUpdatedAtUtc == default)
        {
            throw new InvalidOperationException("Thiếu phiên bản dữ liệu để kiểm tra cập nhật đồng thời.");
        }
    }

    public static bool Matches(
        PayrollInsuranceDeductionConcurrencyInput input) =>
        (input.CurrentUpdatedAtUtc ?? input.CreatedAtUtc) == input.ExpectedUpdatedAtUtc;
}

public sealed record PayrollInsuranceDeductionConcurrencyInput(
    DateTime CreatedAtUtc,
    DateTime? CurrentUpdatedAtUtc,
    DateTime ExpectedUpdatedAtUtc);
