using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;

/// <summary>
/// Validates the invariant shared by the two manually editable workday values.
/// </summary>
public sealed class AttendanceAllowanceWorkdayAdjustmentPolicy
{
    public AttendanceAllowanceValidationResult Validate(UpdateAttendanceAllowanceWorkdaysRequest request)
    {
        if(request.Id == Guid.Empty)
            return Invalid("Thiếu dòng phụ cấp chuyên cần để cập nhật.");

        if(request.StandardWorkdayCount <= 0m)
            return Invalid("Số ngày công chuẩn phải lớn hơn 0.");

        if(request.ActualWorkdayCount < 0m)
            return Invalid("Số ngày công thực tế không được âm.");

        return request.ActualWorkdayCount > request.StandardWorkdayCount
            ? Invalid("Số ngày công thực tế phải từ 0 đến số ngày công chuẩn của kỳ lương.")
            : Valid();
    }

    private static AttendanceAllowanceValidationResult Valid() => new(null);

    private static AttendanceAllowanceValidationResult Invalid(string message) => new(message);
}
