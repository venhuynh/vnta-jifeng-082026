using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;

/// <summary>Single source of truth for leave/holiday allowance request validation.</summary>
public sealed class LeaveHolidayAllowanceRequestValidator : ILeaveHolidayAllowanceRequestValidator
{
    private const int MinimumSupportedYear = 1900;
    private const int MaximumSupportedYear = 2100;
    private const int MaximumNoteLength = 1000;

    public LeaveHolidayAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear) =>
        payrollMonth is >= 1 and <= 12 && payrollYear is >= MinimumSupportedYear and <= MaximumSupportedYear
            ? Valid()
            : Invalid("Kỳ phụ cấp Phép - Lễ không hợp lệ.");

    public LeaveHolidayAllowanceValidationResult Validate(LeaveHolidayAllowanceFilter filter)
    {
        var period = ValidatePeriod(filter.PayrollMonth, filter.PayrollYear);
        if (!period.IsValid)
            return period;

        return filter.Take is >= 1 and <= 5000
            ? Valid()
            : Invalid("Số dòng lấy cho phụ cấp Phép - Lễ phải từ 1 đến 5.000.");
    }

    public LeaveHolidayAllowanceValidationResult Validate(ClearLeaveHolidayAllowanceManualValuesRequest request) =>
        request.PayrollAllowanceSummaryRecordIds is not null
            ? Valid()
            : Invalid("Danh sách dòng xóa dữ liệu nhập tay phụ cấp Phép - Lễ không hợp lệ.");

    public LeaveHolidayAllowanceValidationResult Validate(SyncLeaveHolidayAllowanceFromPreviousMonthRequest request) =>
        ValidatePeriod(request.TargetPayrollMonth, request.TargetPayrollYear);

    public LeaveHolidayAllowanceValidationResult Validate(RecalculateLeaveHolidayAllowanceRequest request)
    {
        var period = ValidatePeriod(request.PayrollMonth, request.PayrollYear);
        return !period.IsValid
            ? period
            : request.PayrollAllowanceSummaryRecordId is { } id && id == Guid.Empty
                ? Invalid("Dòng phụ cấp Phép - Lễ cần tính lại không hợp lệ.")
                : Valid();
    }

    public LeaveHolidayAllowanceValidationResult Validate(UpdateLeaveHolidayAllowanceManualValuesRequest request)
    {
        if (request.PayrollAllowanceSummaryRecordId == Guid.Empty)
            return Invalid("Dòng phụ cấp Phép - Lễ cần điều chỉnh không hợp lệ.");
        if (request.DailyWageAmount < 0 || request.LeaveDayCount < 0 || request.HolidayDayCount < 0)
            return Invalid("Các giá trị phụ cấp Phép - Lễ không được nhỏ hơn 0.");

        return request.Note?.Length > MaximumNoteLength
            ? Invalid("Ghi chú phụ cấp Phép - Lễ không được vượt quá 1.000 ký tự.")
            : Valid();
    }

    public LeaveHolidayAllowanceValidationResult Validate(SetLeaveHolidayAllowanceLockStateRequest request) =>
        request.PayrollAllowanceSummaryRecordId == Guid.Empty
            ? Invalid("Dòng khóa phụ cấp Phép - Lễ không hợp lệ.")
            : Valid();

    public LeaveHolidayAllowanceValidationResult Validate(SetLeaveHolidayAllowanceBatchLockStateRequest request)
    {
        var period = ValidatePeriod(request.PayrollMonth, request.PayrollYear);
        if (!period.IsValid)
            return period;

        // Empty/unknown ids are intentionally allowed through so the command
        // handler can report them as skipped targets, preserving batch semantics.
        return request.PayrollAllowanceSummaryRecordIds is not null
            ? Valid()
            : Invalid("Danh sách dòng khóa phụ cấp Phép - Lễ không hợp lệ.");
    }

    private static LeaveHolidayAllowanceValidationResult Valid() => new(null);

    private static LeaveHolidayAllowanceValidationResult Invalid(string message) => new(message);
}
