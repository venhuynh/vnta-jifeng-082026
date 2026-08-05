using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models;

public sealed class AttendanceDepartmentRecord
{
    const string NonWhitespacePattern = @".*\S.*";
    const string OptionalNonWhitespacePattern = @"^$|.*\S.*";

    public Guid Id { get; set; }

    [Required(ErrorMessage = "Mã phòng ban không được để trống.")]
    [StringLength(50, ErrorMessage = "Mã phòng ban không được vượt quá 50 ký tự.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Mã phòng ban không được chỉ gồm khoảng trắng.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Trung tâm không được để trống.")]
    [StringLength(200, ErrorMessage = "Trung tâm không được vượt quá 200 ký tự.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Trung tâm không được chỉ gồm khoảng trắng.")]
    public string? CenterName { get; set; }

    [Required(ErrorMessage = "Phòng ban/Xưởng không được để trống.")]
    [StringLength(200, ErrorMessage = "Phòng ban/Xưởng không được vượt quá 200 ký tự.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Phòng ban/Xưởng không được chỉ gồm khoảng trắng.")]
    public string? DepartmentOrWorkshopName { get; set; }

    [StringLength(200, ErrorMessage = "Tổ không được vượt quá 200 ký tự.")]
    [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Tổ không được chỉ gồm khoảng trắng.")]
    public string? TeamName { get; set; }

    [StringLength(200, ErrorMessage = "Nhóm không được vượt quá 200 ký tự.")]
    [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Nhóm không được chỉ gồm khoảng trắng.")]
    public string? GroupName { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Ghi chú không được chỉ gồm khoảng trắng.")]
    public string? Notes { get; set; }

    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
