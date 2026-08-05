using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models;

public sealed class AttendanceDepartmentTreeNode
{
    const string NonWhitespacePattern = @".*\S.*";
    const string OptionalNonWhitespacePattern = @"^$|.*\S.*";

    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsDataNode => DepartmentId.HasValue;

    [Required(ErrorMessage = "Mã phòng ban không được để trống.")]
    [StringLength(50, ErrorMessage = "Mã phòng ban không được vượt quá 50 ký tự.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Mã phòng ban không được chỉ gồm khoảng trắng.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Khối không được để trống.")]
    [StringLength(200, ErrorMessage = "Khối không được vượt quá 200 ký tự.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Khối không được chỉ gồm khoảng trắng.")]
    public string? BlockName { get; set; }

    [Required(ErrorMessage = "Phòng ban không được để trống.")]
    [StringLength(200, ErrorMessage = "Phòng ban không được vượt quá 200 ký tự.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Phòng ban không được chỉ gồm khoảng trắng.")]
    public string? DepartmentName { get; set; }

    [StringLength(200, ErrorMessage = "Tổ không được vượt quá 200 ký tự.")]
    [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Tổ không được chỉ gồm khoảng trắng.")]
    public string? TeamName { get; set; }

    [StringLength(200, ErrorMessage = "Nhóm không được vượt quá 200 ký tự.")]
    [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Nhóm không được chỉ gồm khoảng trắng.")]
    public string? GroupName { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Ghi chú không được chỉ gồm khoảng trắng.")]
    public string? Notes { get; set; }

    public int EmployeeCount { get; set; }
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
