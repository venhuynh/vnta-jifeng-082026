using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;

/// <summary>
/// Owns the transport-independent validation rules for the deduction-summary feature.
/// The five detail snapshots remain system calculated; only the "other" component is manual.
/// </summary>
public sealed class PayrollDeductionSummaryRequestValidator : IPayrollDeductionSummaryRequestValidator
{
    public PayrollDeductionSummaryValidationResult ValidatePeriod(int payrollYear, int payrollMonth) =>
        PayrollDeductionSummaryPeriodPolicy.EvaluateRequired(payrollYear, payrollMonth) switch
        {
            PayrollDeductionSummaryPeriodValidationStatus.Supported => Valid(),
            PayrollDeductionSummaryPeriodValidationStatus.YearOutOfRange => Invalid(
                $"Năm dữ liệu phải nằm trong khoảng {PayrollDeductionSummaryPeriodPolicy.MinimumSupportedYear} đến {PayrollDeductionSummaryPeriodPolicy.MaximumSupportedYear}."),
            PayrollDeductionSummaryPeriodValidationStatus.MonthOutOfRange => Invalid("Tháng dữ liệu phải nằm trong khoảng 1 đến 12."),
            _ => Invalid($"Dữ liệu tổng kết khấu trừ bắt đầu từ {PayrollDeductionSummaryPeriodPolicy.MinimumSupportedMonth:00}/{PayrollDeductionSummaryPeriodPolicy.MinimumSupportedYear}.")
        };

    public PayrollDeductionSummaryValidationResult Validate(PayrollDeductionSummaryFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if(filter.PayrollYear.HasValue || filter.PayrollMonth.HasValue)
        {
            var result = filter.PayrollYear.HasValue && filter.PayrollMonth.HasValue
                ? ValidatePeriod(filter.PayrollYear.Value, filter.PayrollMonth.Value)
                : filter.PayrollYear is { } year && (year < PayrollDeductionSummaryPeriodPolicy.MinimumSupportedYear || year > PayrollDeductionSummaryPeriodPolicy.MaximumSupportedYear)
                    ? Invalid("Năm dữ liệu không hợp lệ.")
                    : filter.PayrollMonth is { } month && (month < 1 || month > 12)
                        ? Invalid("Tháng dữ liệu không hợp lệ.")
                        : Valid();
            if(!result.IsValid)
                return result;
        }
        return Valid();
    }

    public PayrollDeductionSummaryValidationResult Validate(PayrollDeductionSummaryExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = ValidatePeriod(request.PayrollYear, request.PayrollMonth);
        return !period.IsValid
            ? period
            : Enum.IsDefined(request.Format) ? Valid() : Invalid("Định dạng xuất tổng kết khấu trừ không hợp lệ.");
    }

    public PayrollDeductionSummaryValidationResult Validate(SyncPayrollDeductionSummaryFromPreviousMonthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ValidatePeriod(request.TargetPayrollYear, request.TargetPayrollMonth);
    }

    public PayrollDeductionSummaryValidationResult Validate(RefreshPayrollDeductionSummaryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(request.SummaryRecordId == Guid.Empty)
            return Invalid("Thiếu định danh dòng tổng kết khấu trừ cần làm mới.");
        if(request.OriginalUpdatedAtUtc == default)
            return Invalid("Thiếu phiên bản dữ liệu để làm mới dòng tổng kết khấu trừ.");
        return ValidatePeriod(request.PayrollYear, request.PayrollMonth);
    }

    public PayrollDeductionSummaryValidationResult Validate(RecalculatePayrollDeductionSummaryPeriodRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ValidatePeriod(request.PayrollYear, request.PayrollMonth);
    }

    public PayrollDeductionSummaryValidationResult Validate(UpdatePayrollDeductionSummaryManualOtherDeductionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(request.Id == Guid.Empty)
            return Invalid("Thiếu định danh dòng tổng kết khấu trừ cần điều chỉnh.");
        if(request.OriginalUpdatedAtUtc == default)
            return Invalid("Thiếu phiên bản dữ liệu để điều chỉnh khoản khấu trừ khác.");
        if(request.Note is { Length: > 1000 })
            return Invalid("Ghi chú khấu trừ không được vượt quá 1.000 ký tự.");
        try { PayrollDeductionSummaryManualOtherDeductionPolicy.Validate(new(request.OtherDeductionAmount)); }
        catch(InvalidOperationException ex) { return Invalid(ex.Message); }
        return Valid();
    }

    public PayrollDeductionSummaryValidationResult Validate(SetPayrollDeductionSummaryLockStateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Id == Guid.Empty ? Invalid("Thiếu định danh dòng tổng kết khấu trừ cần cập nhật.") : Valid();
    }

    public PayrollDeductionSummaryValidationResult Validate(SetPayrollDeductionSummaryBatchLockStateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = ValidatePeriod(request.PayrollYear, request.PayrollMonth);
        if(!period.IsValid)
            return period;
        if(request.PayrollDeductionSummaryRecordIds?.Any(id => id == Guid.Empty) == true
           || request.Items?.Any(item => item.Id == Guid.Empty) == true)
            return Invalid("Danh sách dòng tổng kết khấu trừ có định danh không hợp lệ.");
        var hasIds = request.PayrollDeductionSummaryRecordIds is not null;
        var hasItems = request.Items is not null;
        if(hasIds && hasItems)
        {
            var idSet = request.PayrollDeductionSummaryRecordIds!.ToHashSet();
            var itemSet = request.Items!.Select(item => item.Id).ToHashSet();
            if(!idSet.SetEquals(itemSet))
                return Invalid("Danh sách dòng và danh sách phiên bản phải cùng một phạm vi.");
        }
        if(hasIds && request.PayrollDeductionSummaryRecordIds!.Count == 0)
            return Invalid("Phải chọn ít nhất một dòng hoặc bỏ danh sách để thao tác toàn kỳ.");
        if(hasItems && request.Items!.Count == 0)
            return Invalid("Phải chọn ít nhất một dòng hoặc bỏ danh sách phiên bản để thao tác toàn kỳ.");
        return Valid();
    }

    private static PayrollDeductionSummaryValidationResult Valid() => new(null);
    private static PayrollDeductionSummaryValidationResult Invalid(string message) => new(message);
}
