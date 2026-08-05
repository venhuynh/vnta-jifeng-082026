using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models {
    public sealed class AttendancePositionRecord {
        const string NonWhitespacePattern = @".*\S.*";
        const string OptionalNonWhitespacePattern = @"^$|.*\S.*";

        public Guid Id { get; set; }

        [Required(ErrorMessage = "Mã chức vụ không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã chức vụ không được vượt quá 50 ký tự.")]
        [RegularExpression(NonWhitespacePattern, ErrorMessage = "Mã chức vụ không được chỉ gồm khoảng trắng.")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Tên chức vụ không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên chức vụ không được vượt quá 200 ký tự.")]
        [RegularExpression(NonWhitespacePattern, ErrorMessage = "Tên chức vụ không được chỉ gồm khoảng trắng.")]
        public string? Name { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
        [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Mô tả không được chỉ gồm khoảng trắng.")]
        public string? Description { get; set; }

        public int Status { get; set; }

        public int EmployeeCount { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
