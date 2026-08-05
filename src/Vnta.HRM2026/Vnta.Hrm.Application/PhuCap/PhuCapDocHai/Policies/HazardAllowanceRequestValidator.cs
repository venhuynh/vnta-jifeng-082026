namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Single source of truth for hazard-allowance request validation.</summary>
public sealed class HazardAllowanceRequestValidator : IHazardAllowanceRequestValidator
{
    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;

    public HazardAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear) =>
        payrollMonth is >= 1 and <= 12
        && payrollYear is >= MinimumSupportedYear and <= MaximumSupportedYear
        && (payrollYear != MinimumSupportedYear || payrollMonth >= MinimumSupportedMonth)
            ? Valid()
            : Invalid($"Dữ liệu phụ cấp độc hại bắt đầu từ {MinimumSupportedMonth:00}/{MinimumSupportedYear}.");

    public HazardAllowanceValidationResult Validate(HazardAllowanceFilter filter)
    {
        var periodResult = ValidatePeriod(filter.PayrollMonth, filter.PayrollYear);
        if(!periodResult.IsValid)
            return periodResult;

        return Enum.IsDefined(filter.LockState) && Enum.IsDefined(filter.SummaryBucket)
            ? Valid()
            : Invalid("Điều kiện lọc phụ cấp độc hại không hợp lệ.");
    }

    public HazardAllowanceValidationResult Validate(RefreshHazardAllowanceRequest request)
    {
        var periodResult = ValidatePeriod(request.PayrollMonth, request.PayrollYear);
        return !periodResult.IsValid
            ? periodResult
            : request.PayrollAllowanceSummaryRecordId == Guid.Empty
                ? Invalid("Mã dòng phụ cấp độc hại để làm mới không hợp lệ.")
                : Valid();
    }

    public HazardAllowanceValidationResult Validate(UpdateHazardAllowanceManualValuesRequest request) =>
        request.PayrollAllowanceSummaryRecordId == Guid.Empty
            ? Invalid("Thiếu dòng tổng hợp phụ cấp để điều chỉnh.")
            : Valid();

    public HazardAllowanceValidationResult Validate(SetHazardAllowanceEntitlementBatchRequest request) =>
        request.Targets is not { Count: > 0 }
        || request.Targets.Any(target => target.PayrollAllowanceSummaryRecordId == Guid.Empty
            || target.OriginalDetailUpdatedAtUtc == default
            || target.OriginalSummaryUpdatedAtUtc == default)
        || request.Targets.Select(target => target.PayrollAllowanceSummaryRecordId).Distinct().Count() != request.Targets.Count
            ? Invalid("Dữ liệu cập nhật trạng thái hưởng phụ cấp độc hại không hợp lệ.")
            : Valid();

    public HazardAllowanceValidationResult Validate(SetHazardAllowanceLockStateRequest request) =>
        request.PayrollAllowanceSummaryRecordIds is not { Count: > 0 }
        || request.PayrollAllowanceSummaryRecordIds.Any(id => id == Guid.Empty)
            ? Invalid("Dữ liệu khóa phụ cấp độc hại không hợp lệ.")
            : Valid();

    public HazardAllowanceValidationResult Validate(SetHazardAllowanceBatchLockStateRequest request)
    {
        var periodResult = ValidatePeriod(request.PayrollMonth, request.PayrollYear);
        if(!periodResult.IsValid)
            return periodResult;

        return request.PayrollAllowanceSummaryRecordIds is null
               || request.PayrollAllowanceSummaryRecordIds.All(id => id != Guid.Empty)
            ? Valid()
            : Invalid("Dữ liệu khóa phụ cấp độc hại không hợp lệ.");
    }

    public HazardAllowanceValidationResult Validate(CreateHazardAllowanceExportJobRequest request) =>
        Validate(request.Filter);

    private static HazardAllowanceValidationResult Valid() => new(null);

    private static HazardAllowanceValidationResult Invalid(string message) => new(message);
}
