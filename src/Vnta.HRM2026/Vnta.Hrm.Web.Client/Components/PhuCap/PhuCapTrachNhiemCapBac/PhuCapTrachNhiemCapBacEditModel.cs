using System.ComponentModel.DataAnnotations;
namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemCapBac;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiemCapBacEditModel</c> phục vụ màn hình phụ cấp trách nhiệm cấp bậc.</summary>
public sealed class PhuCapTrachNhiemCapBacEditModel
{
    [Required(ErrorMessage = "Mã bậc không được để trống.")]
    [StringLength(50, ErrorMessage = "Mã bậc không được vượt quá 50 ký tự.")]
    /// <summary>Giá trị <c>Code</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên bậc không được để trống.")]
    [StringLength(200, ErrorMessage = "Tên bậc không được vượt quá 200 ký tự.")]
    /// <summary>Giá trị <c>Name</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Tiền chuẩn phải lớn hơn hoặc bằng 0.")]
    /// <summary>Giá trị <c>StandardResponsibilityAllowanceAmount</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    public decimal StandardResponsibilityAllowanceAmount { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị phải lớn hơn hoặc bằng 0.")]
    /// <summary>Giá trị <c>DisplayOrder</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Giá trị <c>IsActive</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    public bool IsActive { get; set; } = true;

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    /// <summary>Giá trị <c>Note</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Thực hiện xử lý cho luồng <c>CreateDefault</c>.</summary>
    public static PhuCapTrachNhiemCapBacEditModel CreateDefault() => new();

    /// <summary>Thực hiện xử lý cho luồng <c>CreateFrom</c>.</summary>
    public static PhuCapTrachNhiemCapBacEditModel CreateFrom(PayrollResponsibilityAllowanceGradeDto grade) =>
        new()
        {
            Code = grade.Code,
            Name = grade.Name,
            StandardResponsibilityAllowanceAmount = grade.StandardResponsibilityAllowanceAmount,
            DisplayOrder = grade.DisplayOrder,
            IsActive = grade.IsActive,
            Note = grade.Note ?? string.Empty
        };
}
