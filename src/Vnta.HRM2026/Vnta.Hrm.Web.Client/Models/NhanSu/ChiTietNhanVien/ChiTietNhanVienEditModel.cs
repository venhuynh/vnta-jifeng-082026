using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models.NhanSu.ChiTietNhanVien;

public sealed class ChiTietNhanVienEditModel
{
    private const string EmployeeCodePattern = @"^\S{5}$";
    private const string NonWhitespacePattern = @".*\S.*";

    [Required(ErrorMessage = "Mã nhân viên không được để trống.")]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Mã nhân viên phải có đúng 5 ký tự.")]
    [RegularExpression(EmployeeCodePattern, ErrorMessage = "Mã nhân viên không được chứa khoảng trắng.")]
    public string? EmployeeCode { get; set; }

    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Họ tên không được chỉ gồm khoảng trắng.")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Phòng ban không được để trống.")]
    public Guid? DepartmentId { get; set; }

    [Required(ErrorMessage = "Chức vụ không được để trống.")]
    public Guid? PositionId { get; set; }

    public ChiTietNhanVienEmploymentStatus Status { get; set; } = ChiTietNhanVienEmploymentStatus.Probation;

    public DateTime HireDate { get; set; } = DateTime.Today;

    public DateTime? SeniorityStartDate { get; set; }

    public DateTime? ResignedDate { get; set; }

    public DateTime? OriginalUpdatedAtUtc { get; set; }
}
