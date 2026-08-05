using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Exceptions;

namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Policies;

/// <summary>Pure validation for data owned by the allowance-summary aggregate.</summary>
public static class PayrollAllowanceSummaryManualAdjustmentPolicy
{
    public static string? ValidateAndNormalizeNote(UpdatePayrollAllowanceSummaryManualNoteRequest request) =>
        ValidateAndNormalize(new UpdatePayrollAllowanceSummaryManualValuesRequest(
            request.Id, 0m, 0m, 0m, null, 0m, 0m, 0m, 0m, request.Note,
            IsLocked: false, request.OriginalUpdatedAtUtc, request.Actor));

    public static string? ValidateAndNormalize(UpdatePayrollAllowanceSummaryManualValuesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if(request.Id == Guid.Empty)
        {
            throw new PayrollAllowanceSummaryValidationException("Thiếu định danh dòng phụ cấp tổng hợp cần cập nhật.");
        }

        var values = new (string Name, decimal Value)[]
        {
            ("Phụ cấp trách nhiệm", request.ResponsibilityAllowanceAmount),
            ("Phụ cấp trách nhiệm khác", request.ResponsibilityOtherAllowanceAmount),
            ("Phụ cấp thâm niên", request.SeniorityAllowanceAmount),
            ("Phụ cấp cơm", request.MealAllowanceAmount),
            ("Phụ cấp độc hại", request.HazardAllowanceAmount),
            ("Phụ cấp khác", request.OtherAllowanceAmount),
            ("Phụ cấp phép/lễ", request.LeaveHolidayAllowanceAmount)
        };
        var invalidValue = values.FirstOrDefault(value => value.Value < 0m);
        if(invalidValue.Value < 0m)
        {
            throw new PayrollAllowanceSummaryValidationException($"{invalidValue.Name} không được nhỏ hơn 0.");
        }

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if(note is { Length: > 1000 })
        {
            throw new PayrollAllowanceSummaryValidationException("Ghi chú không được vượt quá 1000 ký tự.");
        }

        return note;
    }

    /// <summary>
    /// Keeps the legacy attendance field read-only while older clients migrate away from it.
    /// The allowance-summary aggregate may display this projection but cannot author it.
    /// </summary>
    public static void EnsureAttendanceProjectionIsNotOverridden(
        decimal? legacyRequestedAmount,
        decimal currentProjectionAmount)
    {
        if(legacyRequestedAmount is null || legacyRequestedAmount.Value == currentProjectionAmount)
        {
            return;
        }

        throw new PayrollAllowanceSummaryValidationException(
            "Phụ cấp chuyên cần được tính và quản lý tại màn hình Phụ cấp chuyên cần, không thể điều chỉnh từ tổng hợp phụ cấp.");
    }
}
