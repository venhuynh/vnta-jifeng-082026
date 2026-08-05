using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;

/// <summary>Single source of truth for attendance-allowance request validation.</summary>
public sealed class AttendanceAllowanceRequestValidator : IAttendanceAllowanceRequestValidator
{
    public AttendanceAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear) =>
        AttendanceAllowancePayrollPeriodPolicy.GetValidationError(payrollMonth, payrollYear) is { } errorMessage
            ? Invalid(errorMessage)
            : Valid();

    public AttendanceAllowanceValidationResult Validate(RefreshAttendanceAllowanceRequest request) =>
        ValidatePeriod(request.TargetPayrollMonth, request.TargetPayrollYear);

    public AttendanceAllowanceValidationResult Validate(UpdateAttendanceAllowanceActualWorkdayRequest request)
    {
        if(request.Id == Guid.Empty)
            return Invalid("Thiếu dòng phụ cấp chuyên cần để cập nhật.");

        return request.ActualWorkdayCount < 0
            ? Invalid("Số ngày công thực tế không được âm.")
            : Valid();
    }

    public AttendanceAllowanceValidationResult Validate(UpdateAttendanceAllowanceStandardWorkdayRequest request)
    {
        if(request.Id == Guid.Empty)
            return Invalid("Thiếu dòng phụ cấp chuyên cần để cập nhật.");

        return request.StandardWorkdayCount <= 0
            ? Invalid("Số ngày công chuẩn phải lớn hơn 0.")
            : Valid();
    }

    public AttendanceAllowanceValidationResult Validate(SetAttendanceAllowanceLockStateRequest request) =>
        request.Id == Guid.Empty
            ? Invalid("Thiếu dòng phụ cấp chuyên cần để khóa hoặc mở khóa.")
            : Valid();

    public AttendanceAllowanceValidationResult Validate(SetAttendanceAllowanceBatchLockStateRequest request)
    {
        var periodResult = ValidatePeriod(request.PayrollMonth, request.PayrollYear);
        if(!periodResult.IsValid)
            return periodResult;

        if(!Enum.IsDefined(request.Scope))
            return Invalid("Phạm vi khóa phụ cấp chuyên cần không hợp lệ.");

        if(request.Scope == AttendanceAllowanceBatchLockScope.WholePeriod)
        {
            return request.Items is null
                ? Valid()
                : Invalid("Khóa toàn kỳ không được kèm danh sách dòng.");
        }

        if(request.Items is not { Count: > 0 })
            return Invalid("Phải chọn ít nhất một dòng phụ cấp chuyên cần để khóa hoặc mở khóa.");

        return request.Items.Any(item => item.Id == Guid.Empty)
            ? Invalid("Dữ liệu khóa phụ cấp chuyên cần không hợp lệ.")
            : Valid();
    }

    public AttendanceAllowanceValidationResult Validate(AttendanceAllowanceExportRequest request)
    {
        var periodResult = ValidatePeriod(request.PayrollMonth, request.PayrollYear);
        return !periodResult.IsValid
            ? periodResult
            : Enum.IsDefined(request.Format)
                ? Valid()
                : Invalid("Định dạng xuất phụ cấp chuyên cần không hợp lệ.");
    }

    private static AttendanceAllowanceValidationResult Valid() => new(null);

    private static AttendanceAllowanceValidationResult Invalid(string message) => new(message);
}
