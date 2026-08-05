using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;

/// <summary>Đại diện kiểu <c>PhuCapChuyenCanEditModel</c> phục vụ màn hình phụ cấp chuyên cần.</summary>
public sealed class PhuCapChuyenCanEditModel : IValidatableObject
{
    /// <summary>Giá trị <c>Id</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    public Guid Id { get; init; }

    /// <summary>Giá trị <c>EmployeeDisplay</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    public string EmployeeDisplay { get; init; } = string.Empty;

    /// <summary>Giá trị <c>PayrollPeriodDisplay</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    public string PayrollPeriodDisplay { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "Số ngày công thực tế phải từ 0 đến số ngày công chuẩn của kỳ lương.")]
    /// <summary>Giá trị <c>ActualWorkdayCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    public decimal ActualWorkdayCount { get; set; }

    public decimal OriginalActualWorkdayCount { get; init; }

    /// <summary>Giá trị <c>IsLocked</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    public bool IsLocked { get; init; }

    /// <summary>Giá trị <c>OriginalUpdatedAtUtc</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    public DateTime? OriginalUpdatedAtUtc { get; init; }

    /// <summary>Giá trị <c>StandardWorkdayCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    [CustomValidation(typeof(PhuCapChuyenCanEditModel), nameof(ValidateStandardWorkdayCount))]
    public decimal StandardWorkdayCount { get; set; }

    public decimal OriginalStandardWorkdayCount { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(ActualWorkdayCount > StandardWorkdayCount)
        {
            yield return new ValidationResult(
                "Số ngày công thực tế không được lớn hơn số ngày công chuẩn.",
                [nameof(ActualWorkdayCount), nameof(StandardWorkdayCount)]);
        }
    }

    /// <summary>Kiểm tra số ngày công chuẩn mà không phụ thuộc định dạng số của culture hiện tại.</summary>
    public static ValidationResult? ValidateStandardWorkdayCount(
        object? value,
        ValidationContext validationContext)
    {
        if(value is decimal workdayCount
           && workdayCount > 0m
           && workdayCount <= 9999999999m)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            "Số ngày công chuẩn phải lớn hơn 0.",
            [nameof(StandardWorkdayCount)]);
    }
}
