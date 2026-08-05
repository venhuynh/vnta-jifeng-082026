using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models;

public enum ShiftSchedulingClassificationType
{
    TheoPhongBan = 2,
    TheoNhanVien = 5
}

public enum ShiftSchedulingAssignmentScopeMode
{
    CoDinh = 1,
    TheoKhoangNgay = 2
}

public sealed class ShiftSchedulingSettingRecord : IValidatableObject
{
    private const string NonWhitespacePattern = @".*\S.*";

    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ca làm việc không được để trống.")]
    public Guid? ShiftId { get; set; }

    public string? ShiftCode { get; set; }

    public string? ShiftName { get; set; }

    public string? ShiftStartTime { get; set; }

    public string? ShiftEndTime { get; set; }

    [Required(ErrorMessage = "Phân loại không được để trống.")]
    public ShiftSchedulingClassificationType ClassificationType { get; set; } =
        ShiftSchedulingClassificationType.TheoPhongBan;

    [Required(ErrorMessage = "Hình thức áp dụng không được để trống.")]
    public ShiftSchedulingAssignmentScopeMode AssignmentScopeMode { get; set; } =
        ShiftSchedulingAssignmentScopeMode.CoDinh;

    [Required(ErrorMessage = "Giá trị không được để trống.")]
    [StringLength(500, ErrorMessage = "Giá trị không được vượt quá 500 ký tự.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Giá trị không được chỉ gồm khoảng trắng.")]
    public string? Value { get; set; }

    public DateTime? EffectiveFromDate { get; set; }

    public DateTime? EffectiveToDate { get; set; }

    public List<ShiftSchedulingEmployeeTargetRecord> EmployeeTargets { get; set; } = [];

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string ClassificationTypeText => ClassificationType switch
    {
        ShiftSchedulingClassificationType.TheoPhongBan => "Theo phòng ban",
        ShiftSchedulingClassificationType.TheoNhanVien => "Theo nhân viên",
        _ => "Chưa xác định"
    };

    public string AssignmentScopeModeText => AssignmentScopeMode switch
    {
        ShiftSchedulingAssignmentScopeMode.CoDinh => "Cố định",
        ShiftSchedulingAssignmentScopeMode.TheoKhoangNgay => "Theo khoảng ngày",
        _ => "Chưa xác định"
    };

    public string ActivityStatusText => IsActive ? "Còn hoạt động" : "Ngừng hoạt động";

    public string EffectiveDateRangeText => AssignmentScopeMode switch
    {
        ShiftSchedulingAssignmentScopeMode.CoDinh => "Cố định",
        ShiftSchedulingAssignmentScopeMode.TheoKhoangNgay when EffectiveFromDate.HasValue && EffectiveToDate.HasValue =>
            $"{EffectiveFromDate.Value:dd/MM/yyyy} - {EffectiveToDate.Value:dd/MM/yyyy}",
        ShiftSchedulingAssignmentScopeMode.TheoKhoangNgay => "Chưa chọn khoảng ngày",
        _ => "Chưa xác định"
    };

    public string ShiftDisplayText
    {
        get
        {
            var name = NormalizeDisplayText(ShiftName);
            var startTime = NormalizeDisplayText(ShiftStartTime);
            var endTime = NormalizeDisplayText(ShiftEndTime);
            var title = name ?? "Chưa chọn ca";

            return startTime is not null && endTime is not null
                ? $"{title} ({startTime} - {endTime})"
                : title;
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ShiftId.HasValue && ShiftId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "Ca làm việc không hợp lệ.",
                [nameof(ShiftId)]);
        }

        if (!Enum.IsDefined(ClassificationType))
        {
            yield return new ValidationResult(
                "Phân loại không hợp lệ.",
                [nameof(ClassificationType)]);
        }

        if (!Enum.IsDefined(AssignmentScopeMode))
        {
            yield return new ValidationResult(
                "Hình thức áp dụng không hợp lệ.",
                [nameof(AssignmentScopeMode)]);
        }

        if (AssignmentScopeMode == ShiftSchedulingAssignmentScopeMode.TheoKhoangNgay)
        {
            if (!EffectiveFromDate.HasValue || !EffectiveToDate.HasValue)
            {
                yield return new ValidationResult(
                    "Khoảng ngày áp dụng không được để trống.",
                    [nameof(EffectiveFromDate), nameof(EffectiveToDate)]);
            }
            else if (EffectiveToDate.Value.Date < EffectiveFromDate.Value.Date)
            {
                yield return new ValidationResult(
                    "Đến ngày phải lớn hơn hoặc bằng Từ ngày.",
                    [nameof(EffectiveFromDate), nameof(EffectiveToDate)]);
            }
        }
    }

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ShiftSchedulingEmployeeTargetRecord
{
    public Guid Id { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? DepartmentPath { get; set; }

    public string? PositionName { get; set; }

    public string DisplayText
    {
        get
        {
            var fullName = NormalizeDisplayText(FullName) ?? EmployeeCode;
            var employeeCode = NormalizeDisplayText(EmployeeCode);

            return employeeCode is null
                ? fullName
                : $"{employeeCode} - {fullName}";
        }
    }

    public string Value => DisplayText;

    public string DepartmentDisplayText => NormalizeDisplayText(DepartmentPath) ?? "Chưa có phòng ban";

    public string PositionDisplayText => NormalizeDisplayText(PositionName) ?? "Chưa có chức danh";

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
